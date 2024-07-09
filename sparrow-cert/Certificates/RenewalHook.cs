using System;
using System.Threading.Tasks;
using SparrowCert.Certes;

namespace SparrowCert;

public class RenewalHook : IRenewalHook {
   
   private readonly NotifyConfig notify;
   private readonly string[] domains;

   public RenewalHook(NotifyConfig notifyConfig, string[] domainNames) {
      Console.WriteLine($"{nameof(CertRunner)}:{nameof(RenewalHook)} ctor called");
      notify = notifyConfig;
      domains = domainNames;
   }
   
   public virtual Task OnStartAsync() {
      Console.WriteLine($"{nameof(CertRunner)}:{nameof(RenewalHook)} started");
      return Task.CompletedTask;
   }

   public virtual Task OnStopAsync() {
      Console.WriteLine($"{nameof(CertRunner)}:{nameof(RenewalHook)} stopped");
      return Task.CompletedTask;
   }

   public virtual Task OnRenewalSucceededAsync() {
      Console.WriteLine($"{nameof(CertRunner)}:{nameof(RenewalHook)} succeeded");
      return Task.CompletedTask;
   }

   public virtual Task OnExceptionAsync(Exception e) {
      Console.WriteLine($"{nameof(CertRunner)}:{nameof(RenewalHook)} failed, err={e.Message}");
      return Task.CompletedTask;
   }
}