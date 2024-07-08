using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SparrowCert.Certes;

namespace SparrowCert;

public class SparrowCertRunner(CertConfiguration config) : IHostedService {
   private bool _isStarted = false; 

   public void ConfigureServices(IServiceCollection services) {
      if (config == null) {
         throw new InvalidDataException("no configuration found");
      }
      
      Console.WriteLine($"Starting with '{(config.UseStaging ? "Staging" : "Prod")}' environment");
      Console.WriteLine($"stores at '{(string.IsNullOrEmpty(config.StorePath) ? Environment.CurrentDirectory : config.StorePath)}'");
      services.AddSparrowCert(config);
      services.AddSparrowCertFileCertStore(
         config.Notify,
         config.UseStaging,
         config.StorePath,
         config.CertFriendlyName
      );
      services.AddSparrowCertFileChallengeStore(config.UseStaging, basePath: config.StorePath, config.CertFriendlyName);
      services.AddSparrowCertRenewalHook(config.Notify, config.Domains);
   }

   public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseSparrowCert();
      app.Run(async (context) => {
         await context.Response.WriteAsync($"{nameof(SparrowCertRunner)} is started in '{env}'.");
      });
   }
   
   public Task StartAsync(CancellationToken cancellationToken) {
      Console.WriteLine($"{nameof(SparrowCertRunner)} is starting.");
      _isStarted = true;
      return Task.CompletedTask;
   }

   public Task StopAsync(CancellationToken cancellationToken) {
      Console.WriteLine($"{nameof(SparrowCertRunner)} is stopping.");
      _isStarted = false;
      return Task.CompletedTask;
   }
}