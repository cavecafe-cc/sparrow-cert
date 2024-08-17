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
   CertConfiguration options) : ILetsEncryptClientFactory {

   private const string tag = nameof(LetsEncryptClientFactory);
   private AcmeContext _acme;

   public async Task<ILetsEncryptClient> GetClient() {
      var context = await GetContext();
      return new LetsEncryptClient(context, options);
   }

   private async Task<IAcmeContext> GetContext() {
      if (_acme != null)
         return _acme;

      var existingPrivateKey = await store.GetPrivateKey();
      if (existingPrivateKey != null) {
         Log.Info(tag, "Using existing LetsEncrypt account.");
         var acme = new AcmeContext(options.LetsEncryptUri, existingPrivateKey);
         await acme.Account();
         return _acme = acme;
      }
      else {
         Log.Info(tag, $"Creating LetsEncrypt account with email {options.Email}.");
         var acme = new AcmeContext(options.LetsEncryptUri);
         await acme.NewAccount(options.Email, true);
         await store.SavePrivateKey(acme.AccountKey);
         return _acme = acme;
      }
   }
}