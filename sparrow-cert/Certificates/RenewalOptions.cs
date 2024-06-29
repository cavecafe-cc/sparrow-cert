using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert;

internal class RenewalOptions(ILogger<RenewalOptions> logger) : IConfigureOptions<KestrelServerOptions> {
   public void Configure(KestrelServerOptions options) {
      if (RenewalService.Cert is LetsEncryptX509Cert x509Cert) {
         options.ConfigureHttpsDefaults(o => { o.ServerCertificateSelector = (_a, _b) => x509Cert.GetCertificate(); });
      }
      else if (RenewalService.Cert != null) {
         logger.LogError("This cert cannot be used with Kestrel");
      }
   }
}