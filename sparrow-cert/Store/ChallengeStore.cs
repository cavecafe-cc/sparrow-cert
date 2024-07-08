using System.Collections.Generic;
using System.Threading.Tasks;

namespace SparrowCert.Store;

public delegate Task OnStoreChallenges(IEnumerable<ChallengeInfo> challenges);

public delegate Task OnDeleteChallenges(IEnumerable<ChallengeInfo> challenges);

public delegate Task<IEnumerable<ChallengeInfo>> OnLoadChallenges();

public class ChallengeStore(
   OnStoreChallenges onStore,
   OnLoadChallenges onLoad,
   OnDeleteChallenges onDelete) : IChallengeStore {
   public Task Save(IEnumerable<ChallengeInfo> challenges) {
      return onStore(challenges);
   }

   public Task<IEnumerable<ChallengeInfo>> Load() {
      return onLoad();
   }

   public Task Delete(IEnumerable<ChallengeInfo> challenges) {
      return onDelete(challenges);
   }
}