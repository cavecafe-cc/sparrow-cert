using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;


/// <summary>
/// Cert can be sent to a channel
/// </summary>
public enum ChannelType {
   Slack,
   Email
}

public interface ICertStore {
   
   bool IsStaging { get; init; }
   Task Save(CertType type, IStorableCert cert);
   Task<IKeyCert> GetPrivateKey();
   Task<ICert> GetCert(string pwd);
   Task<bool> NotifyCert(CertType type, byte[] data);
}
