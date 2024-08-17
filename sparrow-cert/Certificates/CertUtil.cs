using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace SparrowCert.Certificates;

public abstract partial class CertUtil
{
   public static string GetDomainOrHostname(string domain) {
      var parts = domain.Split('.');
      if (parts.Length == 2) return domain;
      return parts.Length > 2 ? parts[0] : domain;
   }

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
      File.WriteAllText($"{filePrefix}-chain.pem", chainPem);
      File.WriteAllText($"{filePrefix}-cert.pem", certPem);
      File.WriteAllText($"{filePrefix}-fullchain.pem", fullchainPem);

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
   
   private static readonly Regex FqdnRegex = new(
      @"^(?=.{1,253}$)(?:(?!-)[A-Za-z0-9-]{1,63}(?<!-)\.)+(?:[A-Za-z]{2,})$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

   public static bool IsValidFqdn(string fqdn) {
      return !string.IsNullOrWhiteSpace(fqdn) && FqdnRegex.IsMatch(fqdn);
   }
   
   private static readonly Regex EmailRegex = MyRegex();
   public static bool IsValidEmail(string email) {
      return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
   }

    [GeneratedRegex(@"^(?(""[^\""]+""|([A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*)|(\[(?:[01]?[0-9][0-9]?|2[0-4][0-9]|25[0-5])(?:\.(?:[01]?[0-9][0-9]?|2[0-4][0-9]|25[0-5])){3}\]))@(?([A-Za-z0-9][A-Za-z0-9-]{0,61}[A-Za-z0-9]\.[A-Za-z]{2,})|(\[IPv6:(([0-9A-Fa-f]{1,4}:){7,7}[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,7}:|([0-9A-Fa-f]{1,4}:){1,6}:[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,5}(:[0-9A-Fa-f]{1,4}){1,2}|([0-9A-Fa-f]{1,4}:){1,4}(:[0-9A-Fa-f]{1,4}){1,3}|([0-9A-Fa-f]{1,4}:){1,3}(:[0-9A-Fa-f]{1,4}){1,4}|([0-9A-Fa-f]{1,4}:){1,2}(:[0-9A-Fa-f]{1,4}){1,5}|[0-9A-Fa-f]{1,4}:((:[0-9A-Fa-f]{1,4}){1,6})|:((:[0-9A-Fa-f]{1,4}){1,7}|:)|fe80:(:[0-9A-Fa-f]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]|[1-9]?)?[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]|[1-9]?)?[0-9])|([0-9A-Fa-f]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]|[1-9]?)?[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]|[1-9]?)?[0-9])))\]))$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}