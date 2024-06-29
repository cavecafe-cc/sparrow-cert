using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SparrowCert.Store;

internal class MemoryChallengeStore : IChallengeStore {
   private IEnumerable<ChallengeInfo> _challenges = new List<ChallengeInfo>();

   public Task Delete(IEnumerable<ChallengeInfo> challenges) {
      _challenges = _challenges
         .Where(ca =>
            challenges.All(cb => cb.Token != ca.Token))
         .ToList();

      return Task.CompletedTask;
   }

   public Task Save(IEnumerable<ChallengeInfo> challenges) {
      _challenges = challenges;

      return Task.CompletedTask;
   }

   public Task<IEnumerable<ChallengeInfo>> Load() {
      return Task.FromResult(_challenges);
   }

   public override string ToString() {
      return $"{nameof(MemoryChallengeStore)}: Content {string.Join(",", _challenges)}";
   }
}