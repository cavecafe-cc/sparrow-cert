using System;
using System.Threading.Tasks;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public class CertStore(
   bool isStaging,
   Func<CertType, byte[], Task> onSave,
   Func<CertType, Task<byte[]>> onLoad,
   Func<CertType, byte[], Task<bool>> onNotify) : ICertStore {
   public bool IsStaging { get; init; } = isStaging;

   public Task Save(CertType type, IStorableCert cert) {
      return onSave(type, cert.RawData);
   }

   public async Task<IKeyCert> GetPrivateKey() {
      var bytes = await onLoad(CertType.PrivateKey);
      return bytes == null ? null : new PrivateKey(bytes);
   }

   public async Task<ICert> GetCert(string pwd) {
      var bytes = await onLoad(CertType.PrivateKey);
      return bytes == null ? null : new LetsEncryptX509Cert(bytes, pwd);
   }
   
   public async Task<bool> NotifyCert(CertType type, byte[] data) {
      return await onNotify(type, data);
   }
}