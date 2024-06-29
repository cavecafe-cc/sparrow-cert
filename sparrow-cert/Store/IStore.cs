using System.Threading.Tasks;
using Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public interface IStore {
   
   bool IsStaging { get; init; }
   Task<IKey> GetPrivateKey();
   Task<ChallengeInfo[]> GetChallenges();
   Task<ICert> GetCert(string pwd);
   Task SavePrivateKey(IKey key);
   Task SaveChallenges(ChallengeInfo[] challenges);
   Task SaveCert(IStorableCert cert);
   Task DeleteChallenges(ChallengeInfo[] challenges);
}