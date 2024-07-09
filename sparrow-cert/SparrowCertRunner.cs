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

public class SparrowCertRunner : IHostedService {
   
   private readonly IWebHost host;

   public SparrowCertRunner(SparrowConfiguration cfg) {
      Console.WriteLine($"{nameof(SparrowCertRunner)} ctor called");
      var config = cfg;
      
      #region check certificates
      var certPath = Path.Combine(config.StorePath, $"{config.Domains.First()}.pfx");
      var certExists = File.Exists(certPath);
      var x509 = certExists ? 
         new X509Certificate2(certPath, config.CertPwd) : 
         CertUtil.GenerateSelfSignedCertificate(config.Domains.First());
      var certValid = x509.NotBefore < DateTime.Now && x509.NotAfter > DateTime.Now;
      if (!certValid) {
         throw new InvalidConfigurationException("invalid certificate.");
      }
      #endregion

      host = new WebHostBuilder().UseKestrel(kso => {
         kso.ListenAnyIP(config.HttpPort);
         kso.ListenAnyIP(config.HttpsPort, lo => {
            lo.UseHttps(x509);
         });
      })
      .ConfigureServices(svc => {
         Console.WriteLine($"{nameof(SparrowCertRunner)} ConfigureServices called");
         if (config == null) {
            throw new InvalidDataException("no configuration found");
         }
         Console.WriteLine($"{nameof(SparrowCertRunner)} UseStaging='{config.UseStaging}'");
         Console.WriteLine($"{nameof(SparrowCertRunner)} cert stored at '{(string.IsNullOrEmpty(config.StorePath) ? Environment.CurrentDirectory : config.StorePath)}'");
         svc.AddSparrowCert(config);
         svc.AddSparrowCertFileCertStore(
            config.Notify,
            config.UseStaging,
            config.StorePath,
            config.CertFriendlyName
         );
         svc.AddSparrowCertFileChallengeStore(config.UseStaging, basePath: config.StorePath, config.CertFriendlyName);
         svc.AddSparrowCertRenewalHook(config.Notify, config.Domains);
      })
      .Configure(app =>
      {
         Console.WriteLine($"{nameof(SparrowCertRunner)} Configure called");
         app.UseSparrowCert();
      })
      .Build();
   }
   
   public async Task StartAsync(CancellationToken cancellationToken)
   {
      Console.WriteLine($"{nameof(SparrowCertRunner)} StartAsync called");
      await host.StartAsync(cancellationToken);
   }

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      Console.WriteLine($"{nameof(SparrowCertRunner)} StopAsync called");
      await host.StopAsync(cancellationToken);
   }
}