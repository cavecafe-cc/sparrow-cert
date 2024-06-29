using System.Threading.Tasks;

namespace SparrowCert.Certificates;

public interface ICertProvider {
   Task<RenewalResult> RenewIfNeeded(ICert current = null);
}