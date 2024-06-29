using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Runner;

public abstract class Program {
   
   public static void Main(string[] args) {
      
      var configPath = "cert.json";
      if (args.Length == 1) {
         configPath = args[0];
      }
      else {
         Console.WriteLine("no configPath provided, using current directory (cert.json)");
      }
      if (!File.Exists(configPath)) {
         ShowUsage($"no ConfigPath exists '{configPath}'"); 
         return;
      }

      var config = CertJsonConfiguration.FromFile(configPath);
      if (!config.Domains.Any()) {
         Console.WriteLine($"invalid configuration file (no domains) in {configPath}"); 
         return;
      }
      var buildArgs = SparrowCertStartup.SetConfiguration(config);
      CreateWebHostBuilder(buildArgs).Build().Run();
   }
   
   private static IWebHostBuilder CreateWebHostBuilder(BuilderArgs args) =>
      WebHost.CreateDefaultBuilder()
         .ConfigureLogging(logging =>
            logging.AddFilter(nameof(Microsoft), LogLevel.Warning)
               .AddFilter(nameof(System), LogLevel.Warning)
               .AddFilter(nameof(SparrowCert), LogLevel.Debug)
               .AddConsole())
         .UseKestrel(o => { 
            Console.WriteLine($"Listening on http://*:{args.HttpPort}, https://*:{args.HttpsPort} for domain {args.Domain}.");
            o.ListenAnyIP(args.HttpPort);
            o.ListenAnyIP(args.HttpsPort, listenOptions => {
               // when first run, generate a self-signed certificate for the domain
               listenOptions.UseHttps(CertUtil.GenerateSelfSignedCertificate(args.Domain));
            });
         })
         .UseStartup<SparrowCertStartup>();

   private static void ShowUsage(string msg = "") {
      // to get running assembly name
      var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
      Console.WriteLine($"Usage: \n{assemblyName} [path to cert.json]");
      if (msg.Length <= 0) return;
      Console.WriteLine();
      Console.WriteLine($"msg:'{msg}'");
   }
}