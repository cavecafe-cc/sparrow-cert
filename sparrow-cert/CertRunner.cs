using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.Configuration;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert;

public class CertRunner : IHostedService {
   private const string tag = nameof(CertRunner);

   private readonly IWebHost certHost;
   private static bool IsSelfSigned;

   public CertRunner(CertConfiguration cfg) {
      Log.Entry(tag, "ctor");
      var config = cfg;

      #region check certificates
      if (config.Domains == null || config.Domains.Count == 0) {
         throw new InvalidConfigurationException("no domains found");
      }

      var hostName = CertUtil.GetDomainOrHostname(config.Domains.First());
      var pfx = $"{hostName}.pfx";
      Log.Info(tag, $"searching for '{pfx}'");
      var certPath = Path.Combine(config.StorePath, pfx);
      var certExists = File.Exists(certPath);
      Log.Info(tag, (certExists ? $"cert found at '{certPath}'" : "no cert found, request to create one"));

      // if no certificate found, create a self-signed certificate
      var x509 = certExists ?
         new X509Certificate2(certPath, config.CertPwd) : 
         CertUtil.GenerateSelfSignedCertificate(config.Domains.First());

      IsSelfSigned = !certExists;

      var certValid = x509.NotBefore < DateTime.Now && x509.NotAfter > DateTime.Now;
      if (!certValid) {
         throw new InvalidConfigurationException("invalid certificate.");
      }
      #endregion

      certHost = new WebHostBuilder()
         .UseKestrel(kso => {
            Log.Info(tag, "UseKestrel",
               $"ports http:{config.HttpPort}, https:{config.HttpsPort}");
            kso.ListenAnyIP(config.HttpPort);
            kso.ListenAnyIP(config.HttpsPort, lo => {
               lo.UseHttps(x509);
            });
         })
         .UseUrls($"http://*:{config.HttpPort}", $"https://*:{config.HttpsPort}")
         .ConfigureServices(sevices => {
            Log.Info(tag, "ConfigureServices called");
            if (config == null) {
               throw new InvalidDataException("no configuration found");
            }
            Log.Info(tag, $"UseStaging='{config.UseStaging}'");
            Log.Info(tag, $"cert stored at '{(string.IsNullOrEmpty(config.StorePath) ? Environment.CurrentDirectory : config.StorePath)}'");

            sevices.AddSparrowCert(config);
            sevices.AddSparrowCertFileCertStore(
               config.Notify,
               config.UseStaging,
               config.StorePath,
               hostName
            );
            sevices.Configure<HostOptions>(option => {
               option.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });
            sevices.AddSparrowCertFileChallengeStore(config.UseStaging, basePath: config.StorePath, hostName);
            sevices.AddSparrowCertRenewalHook(config.Notify, config.Domains);
         })
         .Configure(app =>
         {
            Log.Info(tag, "Configure called");
            app.UseSparrowCert();
         })
         .Build();
   }
   
   public async Task StartAsync(CancellationToken cancel) {
      Log.Entry(tag, nameof(StartAsync));

      if (IsSelfSigned) {
         Log.Warn(tag, $"IsSelfSigned={IsSelfSigned}, calling RunOnceAsync");
         await certHost.Services.GetRequiredService<IRenewalService>().RunOnceAsync();
      }
      await certHost.StartAsync(cancel);
   }

   public async Task StopAsync(CancellationToken cancel) {
      Log.Entry(tag, nameof(StopAsync));
      await certHost.StopAsync(cancel);
      certHost.Dispose();
   }

}