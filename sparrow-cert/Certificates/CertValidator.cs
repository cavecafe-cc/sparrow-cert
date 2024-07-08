using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SparrowCert.Certes;

namespace SparrowCert.Certificates;

public interface ICertValidator {
   bool IsValid(ICert cert);
}

public class CertValidator(
   CertConfiguration options,
   ILogger<CertValidator> logger) : ICertValidator {
   public bool IsValid(ICert cert) {
      try {
         if (cert == null)
            return false;

         var now = DateTime.Now;
         logger.LogTrace($"Validating cert UntilExpiry {options.RenewBeforeExpiry}, AfterIssue {options.RenewAfterIssued} - {cert}");
         if (cert.NotAfter - now < options.RenewBeforeExpiry)
            return false;

         if (now - cert.NotBefore > options.RenewAfterIssued)
            return false;

         return cert.NotBefore <= now && cert.NotAfter >= now;
      }
      catch (CryptographicException exc) {
         logger.LogError(exc, "Exception occured during cert validation");
         return false;
      }
   }
}