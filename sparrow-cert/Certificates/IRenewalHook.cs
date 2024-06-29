using System;
using System.Threading.Tasks;

namespace SparrowCert;

public interface IRenewalHook {
   Task OnStartAsync();
   Task OnStopAsync();
   Task OnRenewalSucceededAsync();
   Task OnExceptionAsync(Exception error);
}