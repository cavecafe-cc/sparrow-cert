using Microsoft.Extensions.Logging;
using SparrowCert.Certes;
using SparrowCert.Store;

namespace SparrowCert;

internal abstract class Program {

   public static async Task<int> Main(string[] args) {

      var config = new CertConfiguration();
      var certTask = new CertTask(config);
      return await certTask.RunAsync();

   }
}