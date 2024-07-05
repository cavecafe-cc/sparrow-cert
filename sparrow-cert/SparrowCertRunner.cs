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

public class SparrowCertRunner(string configPath = "") : IHostedService {
   private CertJsonConfiguration _cert { get; } = new(configPath);

   public CertJsonConfiguration GetBuildArgs() {
      return _cert;
   }
   
   public void ConfigureServices(IServiceCollection services) {
      if (_cert == null) {
         throw new InvalidDataException("no configuration found");
      }
      Console.WriteLine($"Starting with '{(_cert.UseStaging ? "Staging" : "Prod")}' environment");
      Console.WriteLine($"stores at '{_cert.StorePath}'");
      
      services.AddSparrowCert(_cert);
      services.AddSparrowCertFileCertStore(
         _cert.Notify, 
         _cert.UseStaging, 
         _cert.StorePath, 
         _cert.CertFriendlyName
      );
      services.AddSparrowCertFileChallengeStore(_cert.UseStaging, basePath:_cert.StorePath, _cert.CertFriendlyName);
      services.AddSparrowCertRenewalHook(_cert.Notify, _cert.Domains);
   }
   
   public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      
      app.UseSparrowCert();
      app.UseAntiforgery();
      app.UseRateLimiter();
      
      app.Run(async (context) => {
         await context.Response.WriteAsync($"{nameof(SparrowCertRunner)} is started in '{env.EnvironmentName}' environment.");
      });
   }

   public Task StartAsync(CancellationToken cancellationToken) {
      throw new NotImplementedException();
   }

   public Task StopAsync(CancellationToken cancellationToken) {
      throw new NotImplementedException();
   }
}
