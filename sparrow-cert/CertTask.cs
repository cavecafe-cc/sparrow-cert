using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using SparrowCert.Certificates;
using SparrowCert.Certes;
using SparrowCert.Store;



namespace SparrowCert;

public enum ExitCodes {
    None = -999,
    GeneralError = -1,
    RenewalDisabled = -100,
    RenewalSuccess = -200,
    RenewFailed = -400,
    InvalidConfiguration = -401,
    NetworkError = -404,
    UnknownException = -500
}

public class CertTask {
    private const string tag = nameof(CertTask);
    private readonly ILogger<CertTask> log = Log.Factory.CreateLogger<CertTask>();

    private IWebHost certHost;
    private static bool isSelfSigned;
    private readonly CertConfiguration config;

    public CertTask(CertConfiguration certConfig) {
        log.LogInformation($"ctor: {tag}");
        config = certConfig;
    }

    public async Task<int> RunAsync() {
        log.LogInformation($"{nameof(RunAsync)} entry");

        try {
            if (!config.Enabled) {
                log.LogWarning("Cert renewal is disabled in configuration. Exiting task ...");
                return (int) ExitCodes.RenewalDisabled;
            }

            if (config.Domains == null || config.Domains.Count == 0) {
                log.LogError("No domains found in configuration. Exiting task ...");
                return (int) ExitCodes.InvalidConfiguration;
            }

            var domainName = config.Domains.First();
            var certPath = Path.Combine(config.KeyConfigPath, $"cert.pfx");
            var certExists = File.Exists(certPath);
            X509Certificate2 x509;
            if (!certExists) {
                log.LogInformation("No existing cert found. Generating self-signed certificate.");
                x509 = CertUtil.GenerateSelfSignedCertificate(domainName);
            }
            else {
                log.LogInformation($"Existing cert found at '{certPath}'. Loading certificate.");
                x509 = new X509Certificate2(certPath, config.CertPwd);
            }
            isSelfSigned = x509.Issuer == x509.Subject;
            var issuedAt = x509.NotBefore;
            var expiry = x509.NotAfter;
            var remainingDays = (int) expiry.Subtract(DateTime.Now).TotalDays;
            log.LogInformation($"Cert is {(isSelfSigned ? "self-signed" : "issued")} at {issuedAt} and expires in {remainingDays} days.");

            var needToRenew = isSelfSigned || remainingDays < config.RenewBeforeExpiry.Days;
            if (needToRenew) {
                log.LogInformation("Cert is Self-signed or nearing expiration, attempting renewal ...");
                await BuildHostAsync(x509);

                var cancel = new CancellationToken();
                var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));  // 1 minute timeout
                var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, timeout.Token);
                await certHost.RunAsync(linkedCancel.Token);
                await certHost.StopAsync(cancel);
                certHost.Dispose();

                log.LogInformation("certHost is disposed.");

                if (FileCertStore.IsStored) {
                    log.LogInformation("Certificate renewal successful.");
                    return (int) ExitCodes.RenewalSuccess;
                }
            }
            else {
                log.LogInformation("Certificate is valid and does not need renewal.");
                return remainingDays;
            }

            // to test permission on /etc/sparrow-cert/cert-test.txt
            var testFile = File.CreateText("/etc/sparrow-cert/cert-test.txt");
            testFile.WriteLine("Hello, etc World!");
            testFile.Close();

            return (int) ExitCodes.RenewFailed;
        }
        catch (Exception e) {
            log.LogError(e, "Error occurred during certificate renewal.");
            return (int) ExitCodes.UnknownException;
        }
    }


    // public async Task StartAsync(CancellationToken cancel) {
    //     Log.Entry(tag, nameof(StartAsync));
    //
    //     if (isSelfSigned) {
    //         Log.Warn(tag, $"isSelfSigned={isSelfSigned}, calling RunOnceAsync");
    //         await certHost.Services.GetRequiredService<IRenewalService>().RunOnceAsync();
    //     }
    //     await certHost.StartAsync(cancel);
    // }
    //
    // public async Task StopAsync(CancellationToken cancel) {
    //     Log.Entry(tag, nameof(StopAsync));
    //     await certHost.StopAsync(cancel);
    //     certHost.Dispose();
    // }

    private async Task BuildHostAsync(X509Certificate2 x509) {
        certHost = new WebHostBuilder()
            .UseKestrel(kso => {
                Log.Info(tag, "UseKestrel",
                    $"ports http:{config.HttpPort}, https:{config.HttpsPort}");
                kso.ListenAnyIP(config.HttpPort);
                kso.ListenAnyIP(config.HttpsPort, lo => { lo.UseHttps(x509); });
            })
            .UseUrls($"http://*:{config.HttpPort}", $"https://*:{config.HttpsPort}")
            .ConfigureServices(services => {
                Log.Info(tag, "ConfigureServices called");
                if (config == null) {
                    throw new InvalidDataException("no configuration found");
                }

                Log.Info(tag, $"UseStaging='{config.UseStaging}'");
                Log.Info(tag, $"cert stored at '{(string.IsNullOrEmpty(config.KeyConfigPath) ? Environment.CurrentDirectory : config.KeyConfigPath)}'");

                var domainName = config.Domains.First();
                services.AddSparrowCert(config);
                services.AddSparrowCertFileCertStore(
                    config.Notify,
                    config.UseStaging,
                    config.KeyConfigPath,
                    domainName
                );
                services.Configure<HostOptions>(option => { option.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore; });
                services.AddSparrowCertFileChallengeStore(config.UseStaging, config.KeyStorePath,  domainName);
                services.AddSparrowCertRenewalHook(config.Notify, config.Domains);
            })
            .Configure(app => {
                Log.Info(tag, "Configure called");
                app.UseSparrowCert();
            })
            .Build();
    }
}