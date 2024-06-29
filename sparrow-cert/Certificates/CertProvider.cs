using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SparrowCert.Certes;
using SparrowCert.Store;

namespace SparrowCert.Certificates;

public class CertProvider : ICertProvider {
   private readonly IStore _store;
   private readonly CertJsonConfiguration _options;
   private readonly ILetsEncryptClientFactory _factory;
   private readonly ICertValidator _validator;
   private readonly ILogger<CertProvider> _logger;

   private readonly string[] _domains;

   public CertProvider(
      CertJsonConfiguration options,
      ICertValidator validator,
      IStore store,
      ILetsEncryptClientFactory factory,
      ILogger<CertProvider> logger) {
      var domains = options.Domains?.Distinct().ToArray();
      if (domains == null || domains.Length == 0) {
         throw new ArgumentException("Invalid domains configuration");
      }
      _options = options;
      _domains = domains;
      _store = store;
      _factory = factory;
      _validator = validator;
      _logger = logger;
   }
   
   private string PrintCertInfo(ICert cert) {
      if (cert == null) {
         return $"\nDomains: [ {string.Join(',', _domains)} ]\nNo certificate found.";
      }
      return $"\nDomains: [ {string.Join(',', _domains)} ]\nThumbprint: {cert.Thumbprint}, \nNotBefore: {cert.NotBefore}, \nNotAfter: {cert.NotAfter}";
   }

   public async Task<RenewalResult> RenewIfNeeded(ICert current = null) {
      
      _logger.LogInformation("Checking renewal required ...");
      if (_validator.IsValid(current)) {
         _logger.LogInformation(
            $"Current cert is valid. Renewal not required.{PrintCertInfo(current)}");
         return new RenewalResult(current, RenewalStatus.Unchanged);
      }

      _logger.LogInformation("Checking saved certs ...");
      var fromStored = await _store.GetCert(_options.CertPwd);
      if (_validator.IsValid(fromStored)) {
         _logger.LogInformation(
            $"A stored & not expired cert was found and will be used{PrintCertInfo(fromStored)}");
         return new RenewalResult(fromStored, RenewalStatus.LoadedFromStore);
      }

      _logger.LogInformation("No valid certificate was found. Requesting new certificate from LetsEncrypt.");
      var newCert = await RequestNewCert();
      _logger.LogInformation($"New certificate was issued{PrintCertInfo(newCert)}");
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