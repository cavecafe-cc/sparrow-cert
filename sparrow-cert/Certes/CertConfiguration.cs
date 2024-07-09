using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Configuration;
using Utils;

namespace SparrowCert.Certes;

public class CertConfiguration {
   
   public const string CERT_JSON = "cert.json";
   
   public CertConfiguration(string configPath = "") {
      if (string.IsNullOrWhiteSpace(configPath)) {
         configPath = CERT_JSON;
      }

      if (!Path.IsPathRooted(configPath)) {
         configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
      }

      var builder = new ConfigurationBuilder()
         .AddJsonFile(configPath, optional: false, reloadOnChange: true);

      var config = builder.Build();

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
      HttpPort = config.GetValue<int>("HttpPort");
      HttpsPort = config.GetValue<int>("HttpsPort");
      CertFriendlyName = config.GetValue<string>("CertFriendlyName");
      StorePath = config.GetValue<string>("StorePath");
      CertPwd = config.GetValue<string>("CertPwd");
      Notify = config.GetSection("Notify").Get<NotifyConfig>();
   }

   // Properties
   public List<string> Domains { get; }
   public string Email { get; }
   public TimeSpan RenewBeforeExpiry { get; }
   public TimeSpan RenewAfterIssued { get; }
   public bool UseStaging { get; }

   [JsonIgnore]
   public Uri LetsEncryptUri => UseStaging
      ? WellKnownServers.LetsEncryptStagingV2
      : WellKnownServers.LetsEncryptV2;

   public CertSigningRequestConfig CertSigningRequest { get; }
   
   public UPnPConfiguration UPnP { get; }
   public RenewalFailMode RenewalFailMode { get; }
   public KeyAlgorithm KeyAlgorithm { get; }
   public TimeSpan RenewalStartupDelay { get; }
   public int HttpPort { get; }
   public int HttpsPort { get; }
   public string CertFriendlyName { get; }
   public string StorePath { get; }
   public string CertPwd { get; }
   public NotifyConfig Notify { get; }
   

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
