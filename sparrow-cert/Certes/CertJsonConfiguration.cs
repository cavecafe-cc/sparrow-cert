using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Certes;
using Certes.Acme;

namespace SparrowCert.Certes;

public class Slack {
   public bool Enabled { get; init; }
   public string[] Channels { get; init; }
   public string Token { get; init; }
   
   public string Body { get; init; } = "Please store the attachment securely, and take the necessary action accordingly.";

}

public class Email {
   public bool Enabled { get; init; }
   public string SenderName { get; init; }
   public string SenderEmail { get; init; }
   public string Recipient { get; init; }
   public string SmtpHost { get; init; }
   public int SmtpPort { get; init; }
   public string SmtpUser { get; init; }
   public string SmtpPwd { get; init; }
   public bool Html { get; init; }
   public string Body { get; init; } = "Please store the attachment securely, and take the necessary action accordingly.";
}

public class Notify {
   public Slack Slack { get; init; }
   public Email Email { get; init; }
}

public class CertJsonConfiguration {
   public IEnumerable<string> Domains { get; init; }

   /// <summary>
   /// Used only for LetsEncrypt to contact you when the domain is about to expire - not actually validated.
   /// </summary>
   public string Email { get; init; }

   /// <summary>
   /// The amount of time before the expiry date of the certificate that a new one is created. Defaults to 30 days.
   /// </summary>
   public TimeSpan? RenewBeforeExpiry { get; init; } = TimeSpan.FromDays(30);

   /// <summary>
   /// The amount of time after the last renewal date that a new one is created. Defaults to null.
   /// </summary>
   public TimeSpan? RenewAfterIssued { get; init; }

   /// <summary>
   /// Recommended while testing - increases your rate limit towards LetsEncrypt. Defaults to false.
   /// </summary>
   public bool UseStaging { get; init; }

   /// <summary>
   /// Gets the uri which will be used to talk to LetsEncrypt servers.
   /// </summary>
      
   [JsonIgnore]
   public Uri LetsEncryptUri => UseStaging
      ? WellKnownServers.LetsEncryptStagingV2
      : WellKnownServers.LetsEncryptV2;

   /// <summary>
   /// Required. Sent to LetsEncrypt to let them know what details you want in your certificate. Some of the properties are optional.
   /// </summary>
   public CsrInfo CertSigningRequest { get; init; }

   /// <summary>
   /// Gets or sets the renewal fail mode - i.e. what happens if an exception is thrown in the certificate renewal process.
   /// </summary>
   public RenewalFailMode RenewalFailMode { get; init; } = RenewalFailMode.LogAndContinue;

   /// <summary>
   /// Gets or sets the <see cref="Certes.KeyAlgorithm"/> used to request a new LetsEncrypt certificate.
   /// </summary>
   public KeyAlgorithm KeyAlgorithm { get; init; } = KeyAlgorithm.ES256;

   /// <summary>
   /// Get or set a delay before the initial run of the renewal service (subsequent runs will be at 1hr intervals)
   /// On some platform/deployment systems (e.g. Azure Slot Swap) we do not want the renewal service to start immediately, because we may not
   /// yet have incoming requests (e.g. for challenges) directed to us. 
   /// </summary>
   public TimeSpan RenewalStartupDelay { get; init; } = TimeSpan.Zero;

   private string _storePath;
   
   public string StorePath
   {
      get => _storePath;
      init {
         _storePath = value;
         if (!string.IsNullOrWhiteSpace(_storePath)) {
            Directory.CreateDirectory(_storePath);
         }
      }
   }
   
   public string CertPwd { get; init; }
   public string CertFriendlyName { get; init; }
   public int HttpPort { get; init; }
   public int HttpsPort { get; init; }
   
   public Notify Notify { get; init; }

   public static CertJsonConfiguration FromJson(string json) {
      return JsonSerializer.Deserialize<CertJsonConfiguration>(json);
   }
   public static string ToJson(CertJsonConfiguration options) {
      return JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true});
   }
      
   public static CertJsonConfiguration FromFile(string configPath) {
      var json = File.ReadAllText(configPath);
      return FromJson(json);
   }
      
   public static void ToFile(CertJsonConfiguration options, string configPath) {
      var json = ToJson(options);
      File.WriteAllText(configPath, json);
   }
}