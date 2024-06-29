using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SparrowCert.Certificates;

public abstract class CertUtil
{
   public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName) {
      using var rsa = RSA.Create(2048);
      var req = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
      var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
      return new X509Certificate2(cert.Export(X509ContentType.Pfx));
   }
   
   public static (string chainPem, string certPem, string fullchainPem) CreatePemFilesFromPfx(string pfxFilePath, string password, string filePrefix) {
      var x509 = new X509Certificate2(pfxFilePath, password, X509KeyStorageFlags.Exportable);
      return CreatePemFilesFromPfx(x509, filePrefix);
   }
   
   public static (string chainPem, string certPem, string fullchainPem) CreatePemFilesFromPfx(X509Certificate2 x509, string filePrefix) {
      var chainPem = GetChainPem(x509);
      var certPem = GetCertPem(x509);
      var fullchainPem = certPem + chainPem;

      // (tk) todo - maybe have another flag in cert.json to decide to store them in the server or not
      // For now, it is not necessary as all operation is done in memory before sending them
      File.WriteAllText($"{filePrefix}_chain.pem", chainPem);
      File.WriteAllText($"{filePrefix}_cert.pem", certPem);
      File.WriteAllText($"{filePrefix}_fullchain.pem", fullchainPem);

      return (chainPem, certPem, fullchainPem);
   }
   
   public static (string chainPem, string certPem, string fullchainPem) CreatePemFilesFromPfx(byte[] rawX509, string filePrefix) {
      var x509 = new X509Certificate2(rawX509);
      return CreatePemFilesFromPfx(x509, filePrefix);
   }

   private static string GetChainPem(X509Certificate2 certificate) {
      var chain = new X509Chain();
      chain.Build(certificate);
    
      var chainPem = new StringBuilder();
      foreach (var element in chain.ChainElements) {
         chainPem.AppendLine("-----BEGIN CERTIFICATE-----");
         chainPem.AppendLine(Convert.ToBase64String(element.Certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
         chainPem.AppendLine("-----END CERTIFICATE-----");
      }
      return chainPem.ToString();
   }

   private static string GetCertPem(X509Certificate2 certificate) {
      var certPem = new StringBuilder();
      certPem.AppendLine("-----BEGIN CERTIFICATE-----");
      certPem.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
      certPem.AppendLine("-----END CERTIFICATE-----");
    
      return certPem.ToString();
   }

}