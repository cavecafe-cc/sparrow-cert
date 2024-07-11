using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpOpenNat;

namespace SparrowCert;

public class UPnPConfiguration {
   public bool Enabled { get; init; }
   public PortMap[]? PortMap { get; init; }

   public static bool IsUpnpEnabled(UPnPConfiguration? upnp) {
      return upnp is { Enabled: true, PortMap.Length: > 0 };
   }

   private static bool IsPortOpen(string host, int port, TimeSpan timeout) {
      try {
         using var client = new TcpClient();
         var result = client.BeginConnect(host, port, null, null);
         var success = result.AsyncWaitHandle.WaitOne(timeout);
         if (!success) {
            return false;
         }

         try {
            client.EndConnect(result);
         }
         catch (SocketException se) {
            // ignored
         }

         return true;
      }
      catch (Exception e) {
         Console.Error.WriteLine($"Failed to check port {port}, err={e.Message}");
         return false;
      }
   }

   public static bool CheckPortsOpened(string domain, IEnumerable<int> ports) {
      return ports.All(port => IsPortOpen(domain, port, TimeSpan.FromSeconds(4)));
   }

   public async Task<bool> OpenPortAsync(string[] msg, int waitSeconds, CancellationToken token) {
      if (!IsUpnpEnabled(this)) return false;

      var result = false;
      var openPorts = Task.Run(async () => {
         try {
            Console.WriteLine(string.Join("\n\t", msg));
            Console.ReadKey(true);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(waitSeconds));
            var device = await NatDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            var ip = await device.GetExternalIPAsync();
            Console.WriteLine($"External IP: {ip}");
            foreach (var map in PortMap!) {
               var protocol = map.Protocol switch {
                  "Tcp" => Protocol.Tcp,
                  "Udp" => Protocol.Udp,
                  _ => throw new NotSupportedException("Protocol not supported")
               };
               var mapping = new Mapping(protocol, map.Internal, map.External, map.Description);
               await device.CreatePortMapAsync(mapping);
               result = true;
            }
         }
         catch (Exception e) {
            await Console.Error.WriteLineAsync($"Failed to open ports using UPnP, err={e.Message}");
            result = false;
         }

         return result;
      }, token);

      try {
         _ = await openPorts.WaitAsync(token);
         return true;
      }
      catch (Exception e) {
         await Console.Error.WriteLineAsync("Failed to open ports using UPnP");
         return false;
      }
   }
}

public class PortMap {
   public int Internal { get; init; }
   public int External { get; init; }
   public string Protocol { get; set; } = "Tcp";
   public string Description { get; set; } = "UPnP";
}