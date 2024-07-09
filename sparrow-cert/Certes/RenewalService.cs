using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SparrowCert.Certificates;
using Utils;
using static SparrowCert.Certificates.RenewalStatus;

namespace SparrowCert.Certes;

public class RenewalService(
   ICertProvider provider,
   IEnumerable<IRenewalHook> hooks,
   IHostApplicationLifetime lifetime,
   ILogger<IRenewalService> logger,
   SparrowConfiguration config)
   : IRenewalService {
   private readonly SemaphoreSlim _semaphore = new(1);
   private Timer _timer;

   internal static ICert Cert { get; private set; }

   public Uri LetsEncryptUri => config.LetsEncryptUri;

   public async Task StartAsync(CancellationToken cancelToken) {
      logger.LogTrace($"{nameof(RenewalService)} StartAsync");
      foreach (var hook in hooks) {
         await hook.OnStartAsync();
      }
      _timer = new Timer(async _ => await RunOnceWithErrorHandlingAsync(), null, Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
      lifetime.ApplicationStarted.Register(() => OnApplicationStarted(cancelToken));
   }

   public async Task StopAsync(CancellationToken cancelToken) {
      logger.LogWarning("The LetsEncrypt middleware's background renewal thread is shutting down.");
      _timer?.Change(Timeout.Infinite, 0);

      foreach (var hook in hooks)
         await hook.OnStopAsync();
   }

   public async Task RunOnceAsync() {
      if (_semaphore.CurrentCount == 0)
         return;

      await _semaphore.WaitAsync();

      try {
         var result = await provider.RenewIfNeeded(Cert);

         if (result.Status != Unchanged) {
            // Preload intermediate certs before exposing certificate to the Kestrel
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            if (result.Cert is LetsEncryptX509Cert x509cert) {
               if (chain.Build(x509cert.GetCertificate())) {
                  logger.LogInformation("Successfully built certificate chain");
               }
               else {
                  logger.LogWarning(
                     "Was not able to build certificate chain. This can cause an outage of your app.");
               }
            }
         }

         Cert = result.Cert;

         if (result.Status == Renewed) {
            foreach (var hook in hooks)
               await hook.OnRenewalSucceededAsync();
         }
      }
      catch (Exception ex) {
         foreach (var hook in hooks)
            await hook.OnExceptionAsync(ex);

         throw;
      }
      finally {
         _semaphore.Release();
      }
   }

   private async Task RunOnceWithErrorHandlingAsync() {
      try {
         logger.LogTrace($"{nameof(RenewalService)} - timer callback starting");
         await RunOnceAsync();
         _timer?.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
      }
      catch (Exception e) when (config.RenewalFailMode != RenewalFailMode.Unhandled) {
         logger.LogWarning(e, $"{nameof(RenewalService)} exception occurred renewing certificates: '{e.Message}'");
         if (config.RenewalFailMode == RenewalFailMode.LogAndRetry) {
            _timer?.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
         }
      }
   }

   private async Task OnApplicationStarted(CancellationToken cancel) {
      logger.LogInformation($"{nameof(RenewalService)} - started");
      
      var isReadyForAcme = UPnPConfiguration.CheckPortsOpened(config.CertFriendlyName, [80, 443]);
      if (!isReadyForAcme) {
         logger.LogWarning($"Domain '{config.CertFriendlyName}' unreachable, renewal is not possible.");
         isReadyForAcme = await config.UPnP.OpenPortAsync(
            [
               "Trying to perform port forwarding, please check the followings.",
               "  1. UPnP is enabled on your network device",
               "  2. No other computers is using port 80 and 443",
               "[NOTE]",
               " Some routers won't allow to open these ports by UPnP.",
               " You can change them by logging into the routers web interface.",
               "",
               "Press any key to continue..."
            ],
            10,
            cancel
         );
      }
      
      if (isReadyForAcme) {
         logger.LogInformation($"Domain '{config.CertFriendlyName}' renewal will be retried every 24 hours");
         _timer?.Change(config.RenewalStartupDelay, TimeSpan.FromDays(1));
      }
      else {
         logger.LogError("Renewal stopped. Please check the network configuration and try again");
         StopAsync(cancel);
      }
   }

   public void Dispose() {
      _timer?.Dispose();
   }
}