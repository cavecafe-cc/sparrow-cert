using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SparrowCert.Certes;
using SparrowCert.Store;

namespace SparrowCert.Certificates;

public class CertProvider : ICertProvider {

   private const string tag = nameof(CertProvider);
   private readonly IStore _store;
   private readonly CertConfiguration _options;
   private readonly ILetsEncryptClientFactory _factory;
   private readonly ICertValidator _validator;

   private readonly string[] _domains;

   public CertProvider(
      CertConfiguration options,
      ICertValidator validator,
      IStore store,
      ILetsEncryptClientFactory factory) {
      var domains = options.Domains?.Distinct().ToArray();
      if (domains == null || domains.Length == 0) {
         throw new ArgumentException("Invalid domains configuration");
      }

      _options = options;
      _domains = domains;
      _store = store;
      _factory = factory;
      _validator = validator;
   }

   private string PrintCertInfo(ICert cert) {
      if (cert == null) {
         return $"\nDomains: [ {string.Join(", ", _domains)} ]\nNo certificate found.";
      }

      return $"\nDomains: [ {string.Join(", ", _domains)} ]\nThumbprint: {cert.Thumbprint}, \nNotBefore: {cert.NotBefore}, \nNotAfter: {cert.NotAfter}";
   }

   public async Task<RenewalResult> RenewIfNeeded(ICert current = null) {
      Log.Info(tag, "Checking renewal required ...");
      if (_validator.IsValid(current)) {
         Log.Info(tag, $"Current cert is valid. Renewal not required.{PrintCertInfo(current)}");
         return new RenewalResult(current, RenewalStatus.Unchanged);
      }

      Log.Info(tag, "Checking saved certs ...");
      var fromStored = await _store.GetCert(_options.CertPwd);
      if (_validator.IsValid(fromStored)) {
         Log.Info(tag, $"A stored & not expired cert was found and will be used{PrintCertInfo(fromStored)}");
         return new RenewalResult(fromStored, RenewalStatus.LoadedFromStore);
      }
      Log.Info(tag, "No valid cert found, requesting new certificate from LetsEncrypt ...");
      var newCert = await RequestNewCert();
      Log.Info(tag, $"New certificate was issued{PrintCertInfo(newCert)}");
      return new RenewalResult(newCert, RenewalStatus.Renewed);
   }

   private async Task<ICert> RequestNewCert() {
      var client = await _factory.GetClient();
      var order = await client.PlaceOrder(_domains);

      await _store.SaveChallenges(order.Challenges);

      try {
         var pfxBytes = await client.FinalizeOrder(order);
         var cert = new LetsEncryptX509Cert(pfxBytes.Bytes, _options.CertPwd);
         await _store.SaveCert(cert);
         return cert;
      }
      finally {
         await _store.DeleteChallenges(order.Challenges);
      }
   }
}