using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SparrowCert.Certes;

namespace SparrowCert.Runner;

public class BuilderArgs(string domain, int httpPort, int httpsPort) {
   public string Domain { get; } = domain;
   public int HttpPort { get; } = httpPort;
   public int HttpsPort { get; } = httpsPort;
}

public class SparrowCertStartup {
   private static CertJsonConfiguration CertJson { get; set; }

   public static BuilderArgs SetConfiguration(CertJsonConfiguration cfg) {
      CertJson = cfg;
      return new BuilderArgs(cfg.Domains.First(), cfg.HttpPort, cfg.HttpsPort);
   }
   public void ConfigureServices(IServiceCollection services) {
      if (CertJson == null) {
         throw new InvalidDataException("no configuration found");
      }
      Console.WriteLine($"Starting with '{(CertJson.UseStaging ? "Staging" : "Prod")}' environment");
      Console.WriteLine($"stores at '{CertJson.StorePath}'");
      
      services.AddSparrowCert(CertJson);
      services.AddSparrowCertFileCertStore(
         CertJson.Notify, 
         CertJson.UseStaging, 
         CertJson.StorePath, 
         CertJson.CertFriendlyName
      );
      services.AddSparrowCertFileChallengeStore(CertJson.UseStaging, basePath:CertJson.StorePath, CertJson.CertFriendlyName);
      services.AddSparrowCertRenewalHook(CertJson.Notify, CertJson.Domains);
   }
   public void Configure(IApplicationBuilder app) {
      app.UseSparrowCert();
      app.Run(async (context) => {
         await context.Response.WriteAsync($"{nameof(Runner)} is running!");
      });
   }

}
