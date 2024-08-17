using System;
using System.Threading.Tasks;
using SparrowCert.Certes;

namespace SparrowCert;

public class RenewalHook : IRenewalHook {
   private const string tag = nameof(RenewalHook);
   private readonly NotifyConfig notify;
   private readonly string[] domains;

   public RenewalHook(NotifyConfig notifyConfig, string[] domainNames) {
      Log.Info(tag, "ctor called" );
      notify = notifyConfig;
      domains = domainNames;
   }
   
   public virtual Task OnStartAsync() {
      Log.Info(tag, $"{nameof(OnStartAsync)} called");;
      return Task.CompletedTask;
   }

   public virtual Task OnStopAsync() {
      Log.Info(tag, $"{nameof(OnStopAsync)} called");
      return Task.CompletedTask;
   }

   public virtual Task OnRenewalSucceededAsync() {
      Log.Info(tag, $"{nameof(OnRenewalSucceededAsync)} called");;
      return Task.CompletedTask;
   }

   public virtual Task OnExceptionAsync(Exception e) {
      Log.Info(tag, $"{nameof(OnExceptionAsync)} called");
      return Task.CompletedTask;
   }
}