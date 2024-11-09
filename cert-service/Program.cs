using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SparrowCert.Certes;

namespace SparrowCert;

public abstract class Program {

   public static void Main(string[] args) {

      var config = new CertConfiguration();
      var builder = WebApplication.CreateBuilder();

      builder.Services.AddSingleton(config);
      builder.Services.AddHostedService<CertRunner>();
      builder.Build().Run();
   }

}