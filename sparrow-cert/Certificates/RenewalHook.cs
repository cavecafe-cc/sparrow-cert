using System;
using System.Threading.Tasks;
using SparrowCert.Certes;

namespace SparrowCert;

public class RenewalHook(Notify notify, string[] domains) : IRenewalHook {
   
   private readonly Notify _notify = notify;
   private readonly string[] _domains = domains;

   public virtual Task OnStartAsync() {
      Console.WriteLine("renewal started");
      return Task.CompletedTask;
   }

   public virtual Task OnStopAsync() {
      Console.WriteLine("renewal stopped");
      return Task.CompletedTask;
   }

   public virtual Task OnRenewalSucceededAsync() {
      Console.WriteLine("renewal succeeded");
      return Task.CompletedTask;
   }

   public virtual Task OnExceptionAsync(Exception error) {
      Console.WriteLine($"renewal failed, error={error.Message}");
      return Task.CompletedTask;
   }
}
