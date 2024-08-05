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
using Sparrow.UPnP;

namespace SparrowCert.Certes;

public class CertConfiguration {
   public const string JSON = "cert.json";
   
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
         Console.WriteLine($"Error saving configuration to '{path}': {e.Message}");
         return false;
      }
   }
   
   public CertConfiguration() { }  // for serialization
   public CertConfiguration(string configPath = "") {
      if (string.IsNullOrWhiteSpace(configPath)) {
         configPath = JSON;
      }

      if (!Path.IsPathRooted(configPath)) {
         configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
      }

      var builder = new ConfigurationBuilder()
         .AddJsonFile(configPath, optional: false, reloadOnChange: true);

      var config = builder.Build();
      Enabled = config.GetValue<bool>("enabled");
      if (!Enabled) return;
      
      Domains = config.GetSection("Domains").Get<List<string>>();
      Email = config.GetValue<string>("Email");
      RenewBeforeExpiry = config.GetValue<TimeSpan>("RenewBeforeExpiry");
      RenewAfterIssued = config.GetValue<TimeSpan>("RenewAfterIssued");
      UseStaging = config.GetValue<bool>("UseStaging");
      CertSigningRequest = config.GetSection("CertSigningRequest").Get<CertSigningRequestConfig>();
      UPnP = config.GetSection("UPnP").Get<UPnPConfiguration>();
      RenewalFailMode = config.GetValue<RenewalFailMode>("RenewalFailMode");
      KeyAlgorithm = config.GetValue<KeyAlgorithm>("KeyAlgorithm");
      RenewalStartupDelay = config.GetValue<TimeSpan>("RenewalStartupDelay");
      
      WithHttpProxy = config.GetValue<bool>("WithHttpProxy");
      HttpPort = config.GetValue<int>("HttpPort");
      HttpsPort = config.GetValue<int>("HttpsPort");
      CertFriendlyName = config.GetValue<string>("CertFriendlyName");
      CertFriendlyName = string.IsNullOrWhiteSpace(CertFriendlyName) ? Domains.First() : CertFriendlyName;
      StorePath = config.GetValue<string>("StorePath");
      CopyCertFiles(StorePath, configPath, Domains.First(), [ "*.pfx", "*.json", "*.pem" ]);
      CertPwd = config.GetValue<string>("CertPwd");
      Notify = config.GetSection("Notify").Get<NotifyConfig>();
   }

   // Copy the files with the given extensions to the store path
   private void CopyCertFiles(string storePath, string configPath, string domain, string[] extensions, bool overwrite = false) {
      if (string.IsNullOrWhiteSpace(storePath) || !Directory.Exists(storePath)) return;
      foreach (var ext in extensions) {
         if (string.IsNullOrWhiteSpace(ext)) continue;
         var dir = Path.GetDirectoryName(configPath);
         if (dir == null) continue;
         var files = Directory.GetFiles(dir, $"{domain}{ext}");
         files.ToList().ForEach(f => {
            try {
               StringBuilder sb = new();
               var filePath = Path.Combine(storePath, Path.GetFileName(f));
               if (File.Exists(filePath)) {
                  sb.Append($"File '{filePath}' already exists, ");
                  if (overwrite) {
                     File.Delete(filePath);
                     sb.Append($"will be replaced with new one.");
                  }
                  else {
                     sb.Append($"using existing one.");
                     Console.WriteLine(sb.ToString());
                     return;
                  }
               }
               sb.Append($"Copying file '{f}' to '{filePath}'");
               File.Copy(f, filePath, overwrite);
               Console.WriteLine(sb.ToString());
            }
            catch (Exception e) {
               Console.WriteLine($"Error copying file '{f}' to '{storePath}': {e.Message}");
            }
         });
      }
   }

   // Properties
   public bool Enabled { get; set; } = false;
   public List<string> Domains { get; set; }
   public string Email { get; set; }
   public TimeSpan RenewBeforeExpiry { get; set; }
   public TimeSpan RenewAfterIssued { get; set; }
   public bool UseStaging { get; set; }

   [JsonIgnore]
   public Uri LetsEncryptUri => UseStaging
      ? WellKnownServers.LetsEncryptStagingV2
      : WellKnownServers.LetsEncryptV2;

   public CertSigningRequestConfig CertSigningRequest { get; set; }
   
   public UPnPConfiguration UPnP { get; set; }
   public RenewalFailMode RenewalFailMode { get; set; }
   public KeyAlgorithm KeyAlgorithm { get; set; }
   public TimeSpan RenewalStartupDelay { get; set; }
   
   public bool WithHttpProxy { get; set; }
   public int HttpPort { get; set; }
   public int HttpsPort { get; set; }
   public string CertFriendlyName { get; set; }
   public string StorePath { get; set; }
   public string CertPwd { get; set; }
   public NotifyConfig Notify { get; set; }
   

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
