using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SparrowCert.Certes;

public interface IRenewalService : IHostedService, IDisposable {
   Uri LetsEncryptUri { get; }
   Task RunOnceAsync();
}