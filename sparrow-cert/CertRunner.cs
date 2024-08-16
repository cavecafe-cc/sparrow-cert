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
   
   private readonly IWebHost host;

   public CertRunner(CertConfiguration cfg) {
      const string tag = nameof(CertRunner);
      Console.WriteLine($"{tag} ctor called");
      var config = cfg;
      
      #region check certificates
      Console.WriteLine($"{tag} checking certificates");
      var pfx = $"{config.Domains.First()}.pfx";
      Console.WriteLine($"{tag} looking for certificate '{pfx}'");
      var certPath = Path.Combine(config.StorePath, pfx);
      Console.WriteLine($"{tag} certPath='{certPath}'");
      var certExists = File.Exists(certPath);
      Console.WriteLine($"{tag} certExists={certExists}");
      var x509 = certExists ? 
         new X509Certificate2(certPath, config.CertPwd) : 
         CertUtil.GenerateSelfSignedCertificate(config.Domains.First());

      var certValid = x509.NotBefore < DateTime.Now && x509.NotAfter > DateTime.Now;
      if (!certValid) {
         throw new InvalidConfigurationException("invalid certificate.");
      }
      #endregion

      host = new WebHostBuilder().UseKestrel(kso => {
         Console.WriteLine($"{nameof(CertRunner)} using local ports http:{config.HttpPort}, https:{config.HttpsPort}");
         kso.ListenAnyIP(config.HttpPort);
         kso.ListenAnyIP(config.HttpsPort, lo => {
            lo.UseHttps(x509);
         });
      })
      .ConfigureServices(svc => {
         Console.WriteLine($"{nameof(CertRunner)} ConfigureServices called");
         if (config == null) {
            throw new InvalidDataException("no configuration found");
         }
         Console.WriteLine($"{nameof(CertRunner)} UseStaging='{config.UseStaging}'");
         Console.WriteLine($"{nameof(CertRunner)} cert stored at '{(string.IsNullOrEmpty(config.StorePath) ? Environment.CurrentDirectory : config.StorePath)}'");
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
         Console.WriteLine($"{nameof(CertRunner)} Configure called");
         app.UseSparrowCert();
      })
      .Build();
   }
   
   public async Task StartAsync(CancellationToken cancel)
   {
      Console.WriteLine($"{nameof(CertRunner)} {nameof(StartAsync)} called");
      await host.StartAsync(cancel);
   }

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      Console.WriteLine($"{nameof(CertRunner)}:StopAsync called");
      await host.StopAsync(cancellationToken);
   }
}