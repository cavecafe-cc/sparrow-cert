using System.Collections.Generic;
using System.Threading.Tasks;

namespace SparrowCert.Store;

public interface IChallengeStore {
   Task Save(IEnumerable<ChallengeInfo> challenges);
   Task<IEnumerable<ChallengeInfo>> Load();
   Task Delete(IEnumerable<ChallengeInfo> challenges);
}