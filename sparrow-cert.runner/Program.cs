using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SparrowCert.Certes;

namespace SparrowCert;

public abstract class Program {
   public static void Main(string[] args) {
      var certConfig = new CertConfiguration("cert.json");
      var builder = WebApplication.CreateBuilder();
      builder.Services.AddSingleton(certConfig);
      builder.Services.AddHostedService<CertRunner>();
      builder.Build().Run();
   }
}
