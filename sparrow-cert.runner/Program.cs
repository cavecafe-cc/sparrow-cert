using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert;

public abstract class Program {
   
   public static void Main(string[] args) {
      
      var sparrow = new SparrowCertRunner();
      var services = new ServiceCollection();
      sparrow.ConfigureServices(services);
      var buildArgs = sparrow.GetBuildArgs();
      
      CreateWebHostBuilder(buildArgs).Build().Run();
   }

   private static IWebHostBuilder CreateWebHostBuilder(CertJsonConfiguration cfg) =>

      WebHost.CreateDefaultBuilder()
         .ConfigureLogging(logging =>
            logging.AddFilter(nameof(Microsoft), LogLevel.Warning)
               .AddFilter(nameof(System), LogLevel.Warning)
               .AddFilter(nameof(SparrowCert), LogLevel.Debug)
               .AddConsole())
         .UseKestrel(ko => {
            Console.WriteLine($"Listening on http://*:{cfg.HttpPort}, https://*:{cfg.HttpsPort} for domain {cfg.Domains.First()}.");
            ko.ListenAnyIP(cfg.HttpPort);
            ko.ListenAnyIP(cfg.HttpsPort, lo => {
               // when first run, generate a self-signed certificate for the domain
               lo.UseHttps(CertUtil.GenerateSelfSignedCertificate(cfg.Domains.First()));
            });
         })
         .UseStartup<Startup>();
}

public class Startup {
   private readonly SparrowCertRunner _sparrow;
   
   public Startup(IConfiguration configuration)
   {
      Configuration = configuration;
      var configPath = "cert.json";
      _sparrow = new SparrowCertRunner(configPath);
   }

   public IConfiguration Configuration { get; }

   public void ConfigureServices(IServiceCollection services)
   {
      // TODO - add your own services here
      
      
      // Add SparrowCert services
      _sparrow.ConfigureServices(services);
   }

   public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
      // TODO - add your own middleware here
      
      
      // Use SparrowCert middleware
      _sparrow.Configure(app, env);
   }

}
