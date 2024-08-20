using System;
using System.Threading.Tasks;
using SparrowCert.Certes;

namespace SparrowCert;

public sealed class RenewalHook : IRenewalHook {
   private const string tag = nameof(RenewalHook);
   private readonly NotifyConfig notify;
   private readonly string[] domains;

   public RenewalHook(NotifyConfig notifyConfig, string[] domainNames) {
      Log.Entry(tag, "ctor" );
      notify = notifyConfig;
      domains = domainNames;
   }
   
   public Task OnStartAsync() {
      Log.Info(tag, $"{nameof(OnStartAsync)} called");;
      return Task.CompletedTask;
   }

   public Task OnStopAsync() {
      Log.Info(tag, $"{nameof(OnStopAsync)} called");
      return Task.CompletedTask;
   }

   public Task OnRenewalSucceededAsync() {
      Log.Info(tag, $"{nameof(OnRenewalSucceededAsync)} called");;
      return Task.CompletedTask;
   }

   public Task OnExceptionAsync(Exception e) {
      Log.Info(tag, $"{nameof(OnExceptionAsync)} called");
      return Task.CompletedTask;
   }
}