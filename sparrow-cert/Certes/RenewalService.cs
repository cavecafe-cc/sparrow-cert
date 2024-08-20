using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SparrowCert.Certificates;

using static SparrowCert.Certificates.RenewalStatus;

namespace SparrowCert.Certes;

public class RenewalService(
   ICertProvider provider,
   IEnumerable<IRenewalHook> hooks,
   IHostApplicationLifetime lifetime,
   CertConfiguration config)
   : IRenewalService {
   private const string tag = nameof(RenewalService);
   private readonly SemaphoreSlim _semaphore = new(1);
   private Timer _timer;

   internal static ICert Cert { get; private set; }

   public Uri LetsEncryptUri => config.LetsEncryptUri;

   public async Task StartAsync(CancellationToken cancel) {
      Log.Info(tag, $"StartAsync called");
      foreach (var hook in hooks) {
         await hook.OnStartAsync();
      }
      _timer = new Timer( _ => RunOnceWithErrorHandlingAsync(), null, Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
      lifetime.ApplicationStarted.Register(() => _ = OnApplicationStarted(cancel));
   }

   public async Task StopAsync(CancellationToken cancelToken) {
      Log.Warn(tag, "StopAsync called");
      _timer?.Change(Timeout.Infinite, 0);
      foreach (var hook in hooks) {
         await hook.OnStopAsync();
      }
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
                  Log.Info(tag, $"Successfully built certificate chain for {x509cert.GetCertificate().Subject}");
               }
               else {
                  Log.Warn(tag, $"Certificate chain build failed for {x509cert.GetCertificate().Subject}");
               }
            }
         }

         Cert = result.Cert;

         if (result.Status == Renewed) {
            foreach (var hook in hooks) {
               await hook.OnRenewalSucceededAsync();
            }
         }
      }
      catch (Exception ex) {
         Log.Catch(tag, nameof(RunOnceAsync), ex);
         foreach (var hook in hooks) {
            await hook.OnExceptionAsync(ex);
         }
         throw;
      }
      finally {
         _semaphore.Release();
      }
   }

   private async Task RunOnceWithErrorHandlingAsync() {
      try {
         Log.Info(tag, "Checking renewal required ...");
         await RunOnceAsync();
         _timer?.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
      }
      catch (Exception e) when (config.RenewalFailMode != RenewalFailMode.Unhandled) {
         Log.Catch(tag, nameof(RunOnceWithErrorHandlingAsync), e);
         if (config.RenewalFailMode == RenewalFailMode.LogAndRetry) {
            _timer?.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
         }
      }
   }

   private async Task OnApplicationStarted(CancellationToken cancel, int waitSeconds = 10) {
      Log.Entry(tag, nameof(OnApplicationStarted));

      // (tk) port todo - testing only
      // var checker = new UPnPChecker(config.UPnP);
      // var dpList = config.UPnP.PortMap!.Select(p => (p.Description, p.External)).ToList();
      // var portAvailabilities = checker.CheckPortsOpened(dpList, config.WithHttpProxy, 4);
      // var notReachable = new List<(string domain, int port)>();
      // var index = 0;
      //
      // foreach (var available in portAvailabilities) {
      //    if (!available) {
      //       var dp = dpList[index];
      //       notReachable.Add(dp);
      //       Log.Info(tag, config.WithHttpProxy ?
      //          $"'{dp.Description}' is not reachable via HTTP proxy port ({UPnPChecker.HTTP_PROXY_PORT})"
      //          : $"'{dp.Description}:{dp.External}' is not reachable");
      //    }
      //    index++;
      // }
      //
      // if (notReachable.Count == 0) {
      //    Log.Info(tag, $"All ports ({string.Join(",", dpList)}) are reachable, starting renewal service");
      //    return;
      // }
      //
      // if (config.UPnP.Enabled) {
      //    var notReachablePorts = notReachable.Select(p => p.port).ToList();
      //    var result = await checker.OpenPortAsync([
      //          "Trying to perform port forwarding, please check the followings.",
      //          "  1. UPnP is enabled on your network device",
      //          $"  2. No other computers are already using the ports {string.Join(',', notReachablePorts)}",
      //          "[NOTE]",
      //          " Some routers won't allow to open these ports by UPnP.",
      //          " You can change them by logging into the routers web interface.",
      //          "",
      //          "Press any key to continue..." ],
      //       waitSeconds,
      //       cancel
      //    );
      //
      //    if (result) {
      //       Log.Info(tag, "Successfully opened ports using UPnP");
      //       _timer?.Change(config.RenewalStartupDelay, TimeSpan.FromDays(1));
      //    }
      // }
      // else {
      //    Log.Warn(tag, "UPnP is disabled, renewal is not possible");
      //    await StopAsync(cancel);
      // }
   }

   public void Dispose() {
      Log.Info(tag, "Dispose called");
      _timer?.Dispose();
   }
}