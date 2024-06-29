using System;
using System.Threading.Tasks;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

internal class MemoryCertStore : ICertStore {
   private IKeyCert _accountCert;
   private ICert _siteCert;

   public bool IsStaging { get; init; } = false;

   public Task Save(CertType type, IStorableCert cert) {
      switch (type) {
         case CertType.PrivateKey:
            _accountCert = (IKeyCert)cert;
            break;
         case CertType.PfxCert:
            _siteCert = cert;
            break;
         default:
            throw new ArgumentException("Unhandled store type", nameof(type));
      }

      return Task.CompletedTask;
   }

   public Task<IKeyCert> GetPrivateKey() {
      return Task.FromResult(_accountCert);
   }

   public Task<ICert> GetCert(string pwd) {
      return Task.FromResult(_siteCert);
   }

   public Task<bool> NotifyCert(CertType type, byte[] data) {
      // do nothing
      return Task.FromResult(false);
   }
}