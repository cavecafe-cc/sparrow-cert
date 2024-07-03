# sparrow-cert

SparrowCert is a simple certificate renewal library for Kestrel-based services. It can be used in any .NET Core-based web service. The service automatically renews certificates, eliminating the need to worry about certificate expiry. It uses the Let's Encrypt ACME protocol to issue and renew certificates and is tested with Kubernetes and Nginx Proxy Manager.

## Features updates

- Focused on making drop-in reusable Nuget package as a library out of the box
- Updated to the .NET Core 8.0
- Updated the old BouncyCastle, Certes dependency to the current version
- Removed old vulnerable (CVE-2024-21907) Newtonsoft.Json dependency, replaced with System.Text.Json
- Customizable configuration of certificate renewal (cert.json)
- Added configurable properties such as CertPath, HttpPort, HttpsPort, CertPwd, and more
- Container friendly (tested with Kubernetes, Docker, and/or NAT Routers, Nginx Proxy)
- Added Auto self-signed certificate generator for initial Kestrel loading
- Built-in Let's Encrypt Staging/Production mode differentiate by the file names
- Added Slack and Email notification with attached certificate and key file upon renewals

## Prerequisites

- .NET Core 8.0
- Docker (optional, for deployment)
- Kubernetes (optional, for deployment)

## How to run it

1. Clone the repository (or get it from NuGet.org)
2. Navigate to the project 'SparrowCert.Runner' directory
3. Create your cert.json file (see below), save it into your project directory (or for testing, SparrowCert.Runner project directory)
4. Run `dotnet build` to build the project
5. Run `dotnet run` to start the service

## Configuration

The service is configured through a `cert.json` file. Here is an example configuration:

**[Note]** Of course, your 'cert.json' file **SHOULD NOT BE** in your repository.

```json
{
  "Domains": [
    "www.own-domain.org",
    "api.own-domain.org"                      // your domain name (include sub-domains, if you have)
  ],
  "Email": "your@email.address",              // your email address for Let's Encrypt account
  "RenewBeforeExpiry": "30.00:00:00",         // renew 30 days before expiry (default 30 days)
  "RenewAfterIssued": "80.00:00:00",          // renew 80 days after issued (default null, which means do nothing)
  "UseStaging": true,                         // trying with Let's Encrypt Staging first
  "CertSigningRequest": {
    "CountryName": "CA",                      // your country code
    "State": "Ontario",                       // your state
    "Locality": "Toronto",                    // your city
    "Organization": "Acme Inc.",              // your organization name
    "OrganizationUnit": "IT",                 // your organization unit (department)
    "CommonName": "own-domain.org"            // your domain name (primary domain)
  },
  "RenewalFailMode": 1,                       // 0: unhandled, 1: continue, 2: retry
  "KeyAlgorithm": 1,                          // 0: RSA256, 1: ES256, 2: ES384, 3: ES512
  "RenewalStartupDelay": "00:00:00",          // delay on startup before renewing

  "HttpPort": 5080,                           // for NAT, customizable HTTP port (80 -> i.e. 5080)
  "HttpsPort": 5443,                          // for NAT, customizable HTTPS port (443 -> i.e. 5443)
  "CertFriendlyName": "own-domain.org",
  "StorePath": "/sparrow-cert/",              // where to store the new certificates (typically, with K8s Persistent Volume)
  "CertPwd": "your-cert-password",            // your certificate password when it is issued (or renewed)

  "Notify": {
    "Slack": {
      "Enabled": true,                        // default false, discarded if not enabled
      "Channels": [ "T0*******HZ" ],          // your Slack channel IDs for notification (get it from Slack channel details)
      "Token": "<your-slack-token>",          // your Slack token, typically starts with 'xoxb-'
      "Body": "\n\n\nPlease store the attachment securely, and take the necessary action accordingly.\n\n\n"
    },
    "Email": {
      "Enabled": true,                        // default false, discarded if not enabled
      "SenderName": "cert-bot",               // your sender name
      "SenderEmail": "<your@email.address>",
      "Recipient": "<recipient@email.address>",
      "SmtpHost": "<your-smtp-server>",
      "SmtpPort": 587,                        // your SMTP port, typically 587
      "SmtpUser": "<your-smtp-user-name>",
      "SmtpPwd": "<your-smtp-password>",
      "Html": false,
      "Body": "\n\n\nPlease store the attachment securely, and take the necessary action accordingly.\n\n\n"
    }
  }
}
```


## How to integrate into your project

```csharp

public static void Main(string[] args) {

    ....
  
  // Load the configuration from the 'cert.json' file      
  var configPath = "<path to your 'cert.json' file>";
  var config = CertJsonConfiguration.FromFile(configPath);
  var buildArgs = SparrowCert.SetConfiguration(config);
   
   
  CreateWebHostBuilder(buildArgs).Build().Run();
}
 
private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
  WebHost.CreateDefaultBuilder(args)
    
    .... 
    
    // Use Kestrel with custom configuration
    .UseKestrel(o => { 
        o.ListenAnyIP(args.HttpPort);
        o.ListenAnyIP(args.HttpsPort, listenOptions => {
           listenOptions.UseHttps(SelfSignedCertGenerator.GenerateCertificate(args.Domain));
    });
        
        
   })
   .UseStartup<SparrowCert>(); // Or add your Startup from inherited SparrowCert


```

## Credit

This project is started from reviewing and trying to utilize [FluffySpoon.AspNet.EncryptWeMust](https://github.com/ffMathy/FluffySpoon.AspNet.EncryptWeMust) and [Certes](https://github.com/fszlin/certes), ended-up refactoring quite a bit for my use cases. Kudos to the authors for the initial implementation.


## References

All the credit for pioneering the approach goes to the authors. I just updated the code for my use cases, and hopping this useful to others.

* [Certes](https://github.com/fszlin/certes): This is the library that I used for the certificate generation and renewal.
* [FluffySpoon.AspNet.EncryptWeMust](https://github.com/ffMathy/FluffySpoon.AspNet.EncryptWeMust): This is the first implementation of using Kestrel for ACME renewal.
* [Let's Encrypt](https://letsencrypt.org/)

## Dependencies

* [Certes](https://github.com/fszlin/certes)
* [BouncyCastle](https://www.bouncycastle.org/) 


## To do

* [ ] Add integration test
* [ ] Check with mobile devices (iOS, Android) for the certificate compatibility ECDHE-RSA-AES256-GCM-SHA384
* [ ] Add certificate renewal notice as Slack message (including delivery of new certificate to the channel)
* [ ] Thinking it runs as one certificate renewal service for multiple services (i.e. as a sidecar container)
