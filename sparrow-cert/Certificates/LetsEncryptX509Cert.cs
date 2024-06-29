using System;
using System.Security.Cryptography.X509Certificates;

namespace SparrowCert.Certificates;

public class LetsEncryptX509Cert : IStorableCert {
   private readonly X509Certificate2 _x509;

   public LetsEncryptX509Cert(X509Certificate2 x509) {
      _x509 = x509;
      RawData = x509.RawData;
   }

   public LetsEncryptX509Cert(byte[] data, string pwd) {
      _x509 = new X509Certificate2(data, pwd);
      RawData = data;
   }

   public DateTime NotAfter => _x509.NotAfter;
   public DateTime NotBefore => _x509.NotBefore;
   public string Thumbprint => _x509.Thumbprint;
   public X509Certificate2 GetCertificate() => _x509;
   public byte[] RawData { get; }

   public override string ToString() {
      return _x509.ToString();
   }
   
   public void CreatePemFiles(string filePrefix) {
      CertUtil.CreatePemFilesFromPfx(_x509, filePrefix);
   }
}