using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace SparrowCert.Certes;

public class CertJsonConfiguration
{
    public CertJsonConfiguration(string configPath = "")
    {
        if (string.IsNullOrWhiteSpace(configPath)) {
            configPath = "cert.json";
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
public class CertSigningRequestConfig
{
    public string CountryName { get; set; }
    public string State { get; set; }
    public string Locality { get; set; }
    public string Organization { get; set; }
    public string OrganizationUnit { get; set; }
    public string CommonName { get; set; }
}

public class NotifyConfig
{
    public SlackConfig Slack { get; set; }
    public EmailConfig Email { get; set; }

    public class SlackConfig
    {
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


//
// public class Slack {
//    public bool Enabled { get; init; }
//    public string[] Channels { get; init; }
//    public string Token { get; init; }
//    
//    public string Body { get; init; } = "Please store the attachment securely, and take the necessary action accordingly.";
//
// }
//
// public class Email {
//    public bool Enabled { get; init; }
//    public string SenderName { get; init; }
//    public string SenderEmail { get; init; }
//    public string Recipient { get; init; }
//    public string SmtpHost { get; init; }
//    public int SmtpPort { get; init; }
//    public string SmtpUser { get; init; }
//    public string SmtpPwd { get; init; }
//    public bool Html { get; init; }
//    public string Body { get; init; } = "Please store the attachment securely, and take the necessary action accordingly.";
// }
//
// public class Notify {
//    public Slack Slack { get; init; }
//    public Email Email { get; init; }
// }
//
// public class CertJsonConfiguration : IConfiguration {
//    
//    private readonly IConfigurationRoot _configuration;
//
//    public CertJsonConfiguration(string configPath)
//    {
//       var builder = new ConfigurationBuilder()
//          .SetBasePath(Directory.GetCurrentDirectory())
//          .AddJsonFile(configPath, optional: false, reloadOnChange: true);
//
//       _configuration = builder.Build();
//
//       // Map configuration properties
//       Domains = _configuration.GetSection("Domains").Get<List<string>>();
//       HttpPort = _configuration.GetValue<int>("HttpPort");
//       HttpsPort = _configuration.GetValue<int>("HttpsPort");
//       UseStaging = _configuration.GetValue<bool>("UseStaging");
//       StorePath = _configuration.GetValue<string>("StorePath");
//       CertFriendlyName = _configuration.GetValue<string>("CertFriendlyName");
//       Notify = _configuration.GetValue<string>("Notify");
//    }
//
//    // Properties
//    public List<string> Domains { get; }
//    public int HttpPort { get; }
//    public int HttpsPort { get; }
//    public bool UseStaging { get; }
//    public string StorePath { get; }
//    public string CertFriendlyName { get; }
//    public string Notify { get; }
//
//    // Implementation of IConfiguration interface
//    public string this[string key] 
//    { 
//       get => _configuration[key];
//       set => _configuration[key] = value;
//    }
//
//    public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();
//
//    public IChangeToken GetReloadToken() => _configuration.GetReloadToken();
//
//    public IConfigurationSection GetSection(string key) => _configuration.GetSection(key);
//    
//    
//    
//    public IEnumerable<string> Domains { get; init; }
//
//    /// <summary>
//    /// Used only for LetsEncrypt to contact you when the domain is about to expire - not actually validated.
//    /// </summary>
//    public string Email { get; init; }
//
//    /// <summary>
//    /// The amount of time before the expiry date of the certificate that a new one is created. Defaults to 30 days.
//    /// </summary>
//    public TimeSpan? RenewBeforeExpiry { get; init; } = TimeSpan.FromDays(30);
//
//    /// <summary>
//    /// The amount of time after the last renewal date that a new one is created. Defaults to null.
//    /// </summary>
//    public TimeSpan? RenewAfterIssued { get; init; }
//
//    /// <summary>
//    /// Recommended while testing - increases your rate limit towards LetsEncrypt. Defaults to false.
//    /// </summary>
//    public bool UseStaging { get; init; }
//
//    /// <summary>
//    /// Gets the uri which will be used to talk to LetsEncrypt servers.
//    /// </summary>
//       
//    [JsonIgnore]
//    public Uri LetsEncryptUri => UseStaging
//       ? WellKnownServers.LetsEncryptStagingV2
//       : WellKnownServers.LetsEncryptV2;
//
//    /// <summary>
//    /// Required. Sent to LetsEncrypt to let them know what details you want in your certificate. Some of the properties are optional.
//    /// </summary>
//    public CsrInfo CertSigningRequest { get; init; }
//
//    /// <summary>
//    /// Gets or sets the renewal fail mode - i.e. what happens if an exception is thrown in the certificate renewal process.
//    /// </summary>
//    public RenewalFailMode RenewalFailMode { get; init; } = RenewalFailMode.LogAndContinue;
//
//    /// <summary>
//    /// Gets or sets the <see cref="Certes.KeyAlgorithm"/> used to request a new LetsEncrypt certificate.
//    /// </summary>
//    public KeyAlgorithm KeyAlgorithm { get; init; } = KeyAlgorithm.ES256;
//
//    /// <summary>
//    /// Get or set a delay before the initial run of the renewal service (subsequent runs will be at 1hr intervals)
//    /// On some platform/deployment systems (e.g. Azure Slot Swap) we do not want the renewal service to start immediately, because we may not
//    /// yet have incoming requests (e.g. for challenges) directed to us. 
//    /// </summary>
//    public TimeSpan RenewalStartupDelay { get; init; } = TimeSpan.Zero;
//
//    private string _storePath;
//    
//    public string StorePath
//    {
//       get => _storePath;
//       init {
//          _storePath = value;
//          if (!string.IsNullOrWhiteSpace(_storePath)) {
//             Directory.CreateDirectory(_storePath);
//          }
//       }
//    }
//    
//    public string CertPwd { get; init; }
//    public string CertFriendlyName { get; init; }
//    public int HttpPort { get; init; }
//    public int HttpsPort { get; init; }
//    
//    public Notify Notify { get; init; }
//
//    public static CertJsonConfiguration FromJson(string json) {
//       return JsonSerializer.Deserialize<CertJsonConfiguration>(json);
//    }
//    public static string ToJson(CertJsonConfiguration options) {
//       return JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true});
//    }
//       
//    public static CertJsonConfiguration FromFile(string configPath = "") {
//       if (configPath == "") {
//          configPath = "cert.json";
//       }
//       var json = File.ReadAllText(configPath);
//       return FromJson(json);
//    }
//       
//    public static void ToFile(CertJsonConfiguration options, string configPath) {
//       var json = ToJson(options);
//       File.WriteAllText(configPath, json);
//    }
//
//    public IConfigurationSection GetSection(string key) {
//       throw new NotImplementedException();
//    }
//
//    public IEnumerable<IConfigurationSection> GetChildren() {
//       throw new NotImplementedException();
//    }
//
//    public IChangeToken GetReloadToken() {
//       throw new NotImplementedException();
//    }
//
//    public string this[string key] {
//       get => throw new NotImplementedException();
//       set => throw new NotImplementedException();
//    }
// }