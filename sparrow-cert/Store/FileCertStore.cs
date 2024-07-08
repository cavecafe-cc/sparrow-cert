using System;
using System.IO;
using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public class FileCertStore(NotifyConfig notify, bool isStaging, string basePath, string filePrefix) : ICertStore {
   public bool IsStaging { get; init; } = isStaging;
   private NotifyConfig _notify { get; init; } = notify;

   public Task Save(CertType type, IStorableCert cert) {
      var path = GetPath(type);
      lock (typeof(FileCertStore)) {
         File.WriteAllBytes(path, cert.RawData);
      }

      if (_notify != null) {
         _ = NotifyCert(type, cert.RawData);
      }
      else {
         Console.Error.WriteLine($"No 'notify' setup for {type}");
      }

      return Task.CompletedTask;
   }

   public async Task<IKeyCert> GetPrivateKey() {
      var bytes = await ReadFile(CertType.PrivateKey);
      return bytes == null ? null : new PrivateKey(bytes);
   }

   public async Task<ICert> GetCert(string pwd) {
      var bytes = await ReadFile(CertType.PfxCert);
      return bytes == null ? null : new LetsEncryptX509Cert(bytes, pwd);
   }

   private Task<byte[]> ReadFile(CertType type) {
      lock (typeof(FileCertStore)) {
         var path = GetPath(type);
         var ret = !File.Exists(path) ? null : File.ReadAllBytes(path);
         return Task.FromResult(ret);
      }
   }

   private string GetPath(CertType type) {
      var fileEnding = type switch {
         CertType.PrivateKey => ("privkey.pem"),
         CertType.PfxCert => ("cert.pfx"),
         _ => throw new NotSupportedException()
      };
      return Path.Combine(basePath, filePrefix + "_" + fileEnding);
   }

   public async Task<bool> NotifyCert(CertType type, byte[] data) {
      if (_notify == null) return false;
      var ret = false;
      if (_notify.Slack is { Enabled: true }) {
         try {
            using var sender = new SlackSender(_notify.Slack, filePrefix);
            ret = await sender.Notify(type, data);
         }
         catch (Exception e) {
            Console.Error.WriteLine(e);
         }
      }

      if (_notify.Email is not { Enabled: true }) return ret;
      try {
         using var sender = new EmailSender(_notify.Email, filePrefix);
         ret = await sender.Notify(type, data);
      }
      catch (Exception e) {
         Console.Error.WriteLine(e);
      }

      return ret;
   }
}