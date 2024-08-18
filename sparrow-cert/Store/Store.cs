using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public enum CertType {
   PrivateKey,
   PfxCert
}

internal partial class Store(
   IEnumerable<ICertStore> certStores,
   IEnumerable<IChallengeStore> challengeStores) : IStore {

   private const string tag = nameof(Store);
   private const string DnsChallengeNameFormat = "_acme-challenge.{0}";
   private const string WildcardRegex = @"^\*\.";
   public bool IsStaging { get; init; }

   public async Task SavePrivateKey(IKey key) {
      await SaveCert(CertType.PrivateKey, new PrivateKey(key), certStores);
   }

   public async Task SaveCert(IStorableCert cert) {
      await SaveCert(CertType.PfxCert, cert, certStores);
      Log.Info(tag, $"Certificate saved for later use. Thumbprint: {cert.Thumbprint}");
   }

   public async Task SaveChallenges(ChallengeInfo[] challenges) {
      Log.Info(tag, $"Saving challenges {challenges}");
      await SaveChallenges(challenges, challengeStores);
   }

   public async Task DeleteChallenges(ChallengeInfo[] challenges) {
      await DeleteChallenges(challenges, challengeStores);
   }

   private string GetChallengeDnsName(string domain) {
      var dnsName = MyRegex().Replace(domain, string.Empty);
      dnsName = string.Format(DnsChallengeNameFormat, dnsName);
      return dnsName;
   }

   private async Task SaveCert(CertType type, IStorableCert cert, IEnumerable<ICertStore> stores) {
      Log.Info(tag, $"Saving {type} certificate in stores");
      var tasks = stores.Select(store => store.Save(type, cert));
      await Task.WhenAll(tasks);
   }

   private async Task SaveChallenges(IEnumerable<ChallengeInfo> challenges, IEnumerable<IChallengeStore> stores) {
      var challengeInfos = challenges.ToList();
      Log.Info(tag, $"Saving challenges ({challengeInfos}) through stores.");

      var list = stores.ToList();
      if (list.Count == 0) {
         Log.Warn(tag, $"There are no challenge stores - challenges will not be stored");
      }

      var tasks = list.Select(store => store.Save(challengeInfos));
      await Task.WhenAll(tasks);
   }

   public async Task<ICert> GetCert(string pwd) {
      foreach (var store in certStores) {
         var certificate = await store.GetCert(pwd);
         if (certificate != null)
            return certificate;
      }

      Log.Info(tag, $"Did not find any cert within stores [{string.Join(",", certStores)}].");
      return null;
   }

   public async Task<IKey> GetPrivateKey() {
      foreach (var store in certStores) {
         var privateKey = await store.GetPrivateKey();
         if (privateKey != null) {
            return privateKey.Key;
         }
      }

      Log.Info(tag, $"no privkey with in stores [{string.Join(",", certStores)}].");
      return null;
   }

   public async Task<ChallengeInfo[]> GetChallenges() {
      var challenges = await LoadChallengesAsync(challengeStores);
      return challenges.ToArray();
   }

   private async Task<IEnumerable<ChallengeInfo>> LoadChallengesAsync(IEnumerable<IChallengeStore> stores) {
      var result = new List<ChallengeInfo>();
      var values = stores.ToList();
      foreach (var strategy in values)
         result.AddRange(await strategy.Load());

      if (result.Count == 0) {
         Log.Warn(tag, $"There are no saved challenges from stores {string.Join(",", values)}");
      }
      else {
         Log.Info(tag, $"Challenges {result} from stores");
      }

      return result;
   }

   private async Task DeleteChallenges(IEnumerable<ChallengeInfo> challenges, IEnumerable<IChallengeStore> stores) {
      var challengeInfos = challenges.ToList();
      Log.Info(tag, $"Deleting challenges {challengeInfos} through stores.");
      var tasks = stores.Select(store => store.Delete(challengeInfos));
      await Task.WhenAll(tasks);
   }

    [GeneratedRegex(WildcardRegex)]
    private static partial Regex MyRegex();
}