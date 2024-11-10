using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.Configuration;
using Sparrow.UPnP;
using SparrowCert.Certificates;

namespace SparrowCert.Certes;

public class CertConfiguration {
   public const string JSON = "cert.json";
   public const string SPARROW_CERT = "sparrow-cert";
   private const string tag = nameof(CertConfiguration);

   #region Properties

   public bool Enabled { get; set; } = false;
   public List<string> Domains { get; set; }
   public string Email { get; set; }
   public TimeSpan RenewBeforeExpiry { get; set; }
   public TimeSpan RenewAfterIssued { get; set; }
   public bool UseStaging { get; set; }
   public CertSigningRequestConfig CertSigningRequest { get; set; }
   public UPnPConfiguration UPnP { get; set; }
   public RenewalFailMode RenewalFailMode { get; set; }
   public KeyAlgorithm KeyAlgorithm { get; set; }
   public TimeSpan RenewalStartupDelay { get; set; }

   public bool WithHttpProxy { get; set; }
   public int HttpPort { get; set; }
   public int HttpsPort { get; set; }
   public string CertAlias { get; set; }
   public string KeyConfigPath { get; set; }
   public string KeyStorePath { get; set; }
   public string CertPwd { get; set; }
   public NotifyConfig Notify { get; set; }

   [JsonIgnore]
   public Uri LetsEncryptUri => UseStaging
      ? WellKnownServers.LetsEncryptStagingV2
      : WellKnownServers.LetsEncryptV2;

   #endregion

   public CertConfiguration(string configPath = "") {
      if (string.IsNullOrWhiteSpace(configPath)) {
         configPath = Path.Combine($"/etc/config/{SPARROW_CERT}", JSON);
      }
      if (!Path.IsPathRooted(configPath)) {
         configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
      }

      var builder = new ConfigurationBuilder()
         .AddJsonFile(configPath, optional: false, reloadOnChange: true);

      var config = builder.Build();
      Enabled = config.GetValue<bool>(nameof(Enabled));
      if (!Enabled) return;

      Domains = config.GetSection(nameof(Domains)).Get<List<string>>();
      Email = config.GetValue<string>(nameof(Email));
      RenewBeforeExpiry = config.GetValue<TimeSpan>( nameof(RenewBeforeExpiry));
      RenewAfterIssued = config.GetValue<TimeSpan>(nameof(RenewAfterIssued));
      UseStaging = config.GetValue<bool>( nameof(UseStaging));
      CertSigningRequest = config.GetSection(nameof(CertSigningRequest)).Get<CertSigningRequestConfig>();
      UPnP = config.GetSection(nameof(UPnP)).Get<UPnPConfiguration>();
      RenewalFailMode = config.GetValue<RenewalFailMode>(nameof(RenewalFailMode));
      KeyAlgorithm = config.GetValue<KeyAlgorithm>(nameof(KeyAlgorithm));
      RenewalStartupDelay = config.GetValue<TimeSpan>(nameof(RenewalStartupDelay));

      WithHttpProxy = config.GetValue<bool>(nameof(WithHttpProxy));
      HttpPort = config.GetValue<int>(nameof(HttpPort));
      HttpsPort = config.GetValue<int>(nameof(HttpsPort));
      CertAlias = config.GetValue<string>(nameof(CertAlias));
      var hostName = CertUtil.GetDomainOrHostname(Domains.First());
      CertAlias = string.IsNullOrWhiteSpace(CertAlias) ? hostName : CertAlias;
      var relPath = config.GetValue<string>( nameof(KeyConfigPath));
      KeyConfigPath = GetOSPath(relPath);
      relPath = config.GetValue<string>(nameof(KeyStorePath));
      KeyStorePath = GetOSPath(relPath);
      CopyCertFiles(KeyConfigPath, configPath, hostName, [ "*.pfx", "*.json", "*.pem" ], true);

      CertPwd = config.GetValue<string>(nameof(CertPwd));
      Notify = config.GetSection("Notify").Get<NotifyConfig>();

   }


   public static CertConfiguration Load(string configPath = "") {
      return new CertConfiguration(configPath);
   }
   
   public bool Save(string path, JsonSerializerOptions options = null) {
      if (string.IsNullOrWhiteSpace(path)) {
         path = JSON;
      }

      if (!Path.IsPathRooted(path)) {
         path = Path.Combine(Directory.GetCurrentDirectory(), path);
      }

      try {
         var json = JsonSerializer.Serialize(this, options);
         File.WriteAllText(path, json);
         return true;
      }
      catch (Exception e) {
         Log.Catch(tag, $"Saving configuration to '{path}'", e);
         return false;
      }
   }

   #region Private Methods

   private string GetOSPath(string relPath) {
      if (string.IsNullOrWhiteSpace(relPath)) {
         throw new InvalidConfigurationException("no keystore path specified");
      }
      if (Path.IsPathRooted(relPath)) {
         return relPath;
      }
      if (OperatingSystem.IsLinux()) {
         return Path.Combine("/var", relPath);
      }
      if (OperatingSystem.IsMacOS() || OperatingSystem.IsWindows()) {
         var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
         return Path.Combine(homeDirectory, relPath);
      }
      throw new InvalidConfigurationException("unsupported operating system");
   }

   // Copy the files with the given extensions to the store path
   private void CopyCertFiles(string keyPath, string configPath, string hostName, string[] extensions, bool backupReplace = false) {
      if (string.IsNullOrWhiteSpace(keyPath) || !Directory.Exists(keyPath)) return;
      foreach (var ext in extensions) {
         if (string.IsNullOrWhiteSpace(ext)) continue;
         var dir = Path.GetDirectoryName(configPath);
         if (dir == null) continue;
         var files = Directory.GetFiles(dir, $"{hostName}{ext}");
         files.ToList().ForEach(f => {
            try {
               StringBuilder sb = new();
               var filePath = Path.Combine(keyPath, Path.GetFileName(f));
               if (File.Exists(filePath)) {
                  sb.Append($"File '{filePath}' already exists, ");
                  if (backupReplace) {
                     var backup = Path.Combine(keyPath, $"{Path.GetFileNameWithoutExtension(f)}-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(f)}");
                     File.Move(filePath, backup);
                     sb.Append($"existing file moved to '{backup}' and ");
                     sb.Append($"will be replaced with new one.");
                  }
                  else {
                     sb.Append($"using existing one.");
                     Log.Info(tag, sb.ToString());
                     return;
                  }
               }
               sb.Append($"Copying file '{f}' to '{filePath}'");
               File.Copy(f, filePath, backupReplace);
               Log.Info(tag, sb.ToString());
            }
            catch (Exception e) {
               Log.Catch(tag, $"Error copying file '{f}' to '{keyPath}'", e);
            }
         });
      }
   }

   #endregion

}

// Nested Classes
public class CertSigningRequestConfig {
   public string CountryName { get; set; }
   public string State { get; set; }
   public string Locality { get; set; }
   public string Organization { get; set; }
   public string OrganizationUnit { get; set; }
   public string CommonName { get; set; }
}

public class NotifyConfig {
   public SlackConfig Slack { get; set; }
   public EmailConfig Email { get; set; }

   public class SlackConfig {
      public bool Enabled { get; set; }
      public List<string> Channels { get; set; }
      public string Token { get; set; }
      public string Body { get; set; }
   }

   public class EmailConfig {
      public bool Enabled { get; set; }
      public string SenderName { get; set; }
      public string SenderEmail { get; set; }
      public string Recipient { get; set; }
      public string SmtpHost { get; set; }
      public int SmtpPort { get; set; }
      public string SmtpUser { get; set; }
      public string SmtpPwd { get; set; }
      public bool Html { get; set; }
      public string Body { get; set; }
   }
}