using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.Configuration;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert;

public class CertRunner : IHostedService {

   const string tag = nameof(CertRunner);
   private readonly IWebHost host;

   public CertRunner(CertConfiguration cfg) {

      Log.Info(tag, "ctor called");
      var config = cfg;

      #region check certificates
      if (config.Domains == null || config.Domains.Count == 0) {
         throw new InvalidConfigurationException("no domains found");
      }
      var pfx = $"{config.Domains.First()}.pfx";
      Log.Info(tag, $"searching for '{pfx}'");
      var certPath = Path.Combine(config.StorePath, pfx);
      var certExists = File.Exists(certPath);
      Log.Info(tag, (certExists ? $"cert found at '{certPath}'" : "no cert found, request to create one"));

      var x509 = certExists ?
         new X509Certificate2(certPath, config.CertPwd) : 
         CertUtil.GenerateSelfSignedCertificate(config.Domains.First());

      var certValid = x509.NotBefore < DateTime.Now && x509.NotAfter > DateTime.Now;
      if (!certValid) {
         throw new InvalidConfigurationException("invalid certificate.");
      }
      #endregion

      host = new WebHostBuilder().UseKestrel(kso => {
         Log.Info(tag, "UseKestrel called",
            $"using local ports http:{config.HttpPort}, https:{config.HttpsPort}");
         kso.ListenAnyIP(config.HttpPort);
         kso.ListenAnyIP(config.HttpsPort, lo => {
            lo.UseHttps(x509);
         });
      })
      .ConfigureServices(svc => {
         Log.Info(tag, "ConfigureServices called");
         if (config == null) {
            throw new InvalidDataException("no configuration found");
         }
         Log.Info(tag, $"UseStaging='{config.UseStaging}'");
         Log.Info(tag, $"cert stored at '{(string.IsNullOrEmpty(config.StorePath) ? Environment.CurrentDirectory : config.StorePath)}'");

         svc.AddSparrowCert(config);
         svc.AddSparrowCertFileCertStore(
            config.Notify,
            config.UseStaging,
            config.StorePath,
            config.Domains.First()
         );
         svc.AddSparrowCertFileChallengeStore(config.UseStaging, basePath: config.StorePath, config.Domains.First());
         svc.AddSparrowCertRenewalHook(config.Notify, config.Domains);
      })
      .Configure(app =>
      {
         Log.Info(tag, "Configure called");
         app.UseSparrowCert();
      })
      .Build();
   }
   
   public async Task StartAsync(CancellationToken cancel)
   {
      Log.Info(tag, "starting ...");
      await host.StartAsync(cancel);
   }

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      Log.Info(tag, "stopping ...");
      await host.StopAsync(cancellationToken);
   }
}