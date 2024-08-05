using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Sparrow.UPnP;
using SparrowCert.Certificates;

using static SparrowCert.Certificates.RenewalStatus;

namespace SparrowCert.Certes;

public class RenewalService(
   ICertProvider provider,
   IEnumerable<IRenewalHook> hooks,
   IHostApplicationLifetime lifetime,
   ILogger<IRenewalService> logger,
   CertConfiguration config)
   : IRenewalService {
   private readonly SemaphoreSlim _semaphore = new(1);
   private Timer _timer;

   internal static ICert Cert { get; private set; }

   public Uri LetsEncryptUri => config.LetsEncryptUri;

   public async Task StartAsync(CancellationToken cancel) {
      logger.LogTrace($"{nameof(RenewalService)} StartAsync");
      foreach (var hook in hooks) {
         await hook.OnStartAsync();
      }
      _timer = new Timer(async _ => await RunOnceWithErrorHandlingAsync(), null, Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
      lifetime.ApplicationStarted.Register(() => _ = OnApplicationStarted(cancel));
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

   private async Task OnApplicationStarted(CancellationToken cancel, int waitSeconds = 10) {
      logger.LogInformation($"{nameof(RenewalService)} - started");
      
      var checker = new UPnPChecker(config.UPnP);
      var dpList = config.UPnP.PortMap!.Select(p => (p.Description, p.External)).ToList();
      var portAvailabilities = checker.CheckPortsOpened(dpList);
      var notReachable = new List<(string domain, int port)>();
      var index = 0;
      
      foreach (var available in portAvailabilities) {
         if (!available) {
            var dp = dpList[index];
            notReachable.Add(dp);
            Console.WriteLine($"'{dp.Description}:{dp.External}' is not reachable");
         }
         index++;
      }
      
      if (notReachable.Count == 0) {
         logger.LogInformation($"'{string.Join(",", dpList)}' is reachable from outside of your network");
         return;
      }
      
      if (config.UPnP.Enabled) {
         var notReachablePorts = notReachable.Select(p => p.port).ToList();
         var result = await checker.OpenPortAsync([
               "Trying to perform port forwarding, please check the followings.",
               "  1. UPnP is enabled on your network device",
               $"  2. No other computers are already using the ports {string.Join(',', notReachablePorts)}",
               "[NOTE]",
               " Some routers won't allow to open these ports by UPnP.",
               " You can change them by logging into the routers web interface.",
               "",
               "Press any key to continue..." ],
            waitSeconds,
            cancel
         );

         if (result) {
            logger.LogInformation("Successfully opened ports using UPnP");
            _timer?.Change(config.RenewalStartupDelay, TimeSpan.FromDays(1));
         }
      }
      else {
         logger.LogWarning("UPnP is disabled, renewal is not possible");
         await StopAsync(cancel);
      }
   }

   public void Dispose() {
      _timer?.Dispose();
   }
}