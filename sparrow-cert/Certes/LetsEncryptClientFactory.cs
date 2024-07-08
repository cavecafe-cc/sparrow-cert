using System.Threading.Tasks;
using Certes;
using Microsoft.Extensions.Logging;
using SparrowCert.Store;

namespace SparrowCert.Certes;

public interface ILetsEncryptClientFactory {
   Task<ILetsEncryptClient> GetClient();
}

public class LetsEncryptClientFactory(
   IStore store,
   CertConfiguration options,
   ILoggerFactory loggerFactory) : ILetsEncryptClientFactory {
   private readonly ILogger _logger = loggerFactory.CreateLogger<LetsEncryptClientFactory>();
   private AcmeContext _acme;

   public async Task<ILetsEncryptClient> GetClient() {
      var context = await GetContext();
      var logger = loggerFactory.CreateLogger<LetsEncryptClient>();
      return new LetsEncryptClient(context, options, logger);
   }

   private async Task<IAcmeContext> GetContext() {
      if (_acme != null)
         return _acme;

      var existingPrivateKey = await store.GetPrivateKey();
      if (existingPrivateKey != null) {
         _logger.LogDebug("Using existing LetsEncrypt account.");
         var acme = new AcmeContext(options.LetsEncryptUri, existingPrivateKey);
         await acme.Account();
         return _acme = acme;
      }
      else {
         _logger.LogDebug($"Creating LetsEncrypt account with email {options.Email}.");
         var acme = new AcmeContext(options.LetsEncryptUri);
         await acme.NewAccount(options.Email, true);
         await store.SavePrivateKey(acme.AccountKey);
         return _acme = acme;
      }
   }
}