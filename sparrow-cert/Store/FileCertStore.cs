using System;
using System.IO;
using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public class FileCertStore(NotifyConfig notify, bool isStaging, string basePath, string filePrefix) : ICertStore {
   private const string tag = nameof(FileCertStore);
   public bool IsStaging { get; init; } = isStaging;
   private NotifyConfig _notify { get; init; } = notify;

   public Task Save(CertType type, IStorableCert cert) {
      var path = GetFilePath(type);
      lock (typeof(FileCertStore)) {
         Directory.CreateDirectory(basePath);
         Log.Info(tag, $"Saving {type} certificate to {path}");
         File.WriteAllBytes(path, cert.RawData);
      }

      if (_notify != null) {
         _ = NotifyCert(type, cert.RawData);
      }
      else {
         Log.Error(tag, $"No 'notify' setup for {type}");
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
         var path = GetFilePath(type);
         Log.Info(tag, $"Try get {type} certificate from {path}");
         var ret = !File.Exists(path) ? null : File.ReadAllBytes(path);
         return Task.FromResult(ret);
      }
   }

   private string GetFilePath(CertType type) {
      var fileEnding = type switch {
         CertType.PrivateKey => ("-privkey.pem"),
         CertType.PfxCert => (".pfx"),
         _ => throw new NotSupportedException()
      };
      return Path.Combine(basePath, filePrefix + fileEnding);
   }

   public async Task<bool> NotifyCert(CertType type, byte[] data) {
      const string func = nameof(NotifyCert);
      Log.Entry(tag, func);
      if (_notify == null) {
         Log.Warn(tag, func, "No notify configuration found");
         return false;
      }
      var ret = false;
      if (_notify.Slack is { Enabled: true }) {
         try {
            using var sender = new SlackSender(_notify.Slack, filePrefix);
            ret = await sender.Notify(type, data);
         }
         catch (Exception e) {
            Log.Catch(tag, func, e);
         }
      }

      if (_notify.Email is { Enabled: true }) {
         try {
            using var sender = new EmailSender(_notify.Email, filePrefix);
            ret = await sender.Notify(type, data);
         }
         catch (Exception e) {
            Log.Catch(tag, func, e);
         }
      }

      return ret;
   }
}