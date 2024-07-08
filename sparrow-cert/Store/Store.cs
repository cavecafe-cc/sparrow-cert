using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes;
using Microsoft.Extensions.Logging;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public enum CertType {
   PrivateKey,
   PfxCert
}

internal class Store(
   IEnumerable<ICertStore> certStores,
   IEnumerable<IChallengeStore> challengeStores,
   ILogger<IStore> logger) : IStore {
   private const string DnsChallengeNameFormat = "_acme-challenge.{0}";
   private const string WildcardRegex = @"^\*\.";
   public bool IsStaging { get; init; }

   public async Task SavePrivateKey(IKey key) {
      await SaveCert(CertType.PrivateKey, new PrivateKey(key), certStores);
   }

   public async Task SaveCert(IStorableCert cert) {
      await SaveCert(CertType.PfxCert, cert, certStores);
      logger.LogInformation("Certificate saved for later use.");
   }

   public async Task SaveChallenges(ChallengeInfo[] challenges) {
      logger.LogTrace($"Using ({challengeStores}) for saving challenge");
      await SaveChallenges(challenges, challengeStores);
   }

   public async Task DeleteChallenges(ChallengeInfo[] challenges) {
      await DeleteChallenges(challenges, challengeStores);
   }

   private string GetChallengeDnsName(string domain) {
      var dnsName = Regex.Replace(domain, WildcardRegex, String.Empty);
      dnsName = String.Format(DnsChallengeNameFormat, dnsName);
      return dnsName;
   }

   private async Task SaveCert(CertType type, IStorableCert cert, IEnumerable<ICertStore> stores) {
      logger.LogTrace($"Saving {type} certificate in stores");

      var tasks = stores.Select(store => store.Save(type, cert));

      await Task.WhenAll(tasks);
   }

   private async Task SaveChallenges(IEnumerable<ChallengeInfo> challenges, IEnumerable<IChallengeStore> stores) {
      logger.LogTrace("Saving challenges ({challenges}) through stores.", challenges);

      var list = stores.ToList();
      if (list.Count == 0) {
         logger.LogWarning("There are no challenges in stores - challenges will not be stored");
      }

      var tasks = list.Select(store => store.Save(challenges));
      await Task.WhenAll(tasks);
   }

   public async Task<ICert> GetCert(string pwd) {
      foreach (var store in certStores) {
         var certificate = await store.GetCert(pwd);
         if (certificate != null)
            return certificate;
      }

      logger.LogTrace($"Did not find any cert within stores [{string.Join(",", certStores)}].");
      return null;
   }

   public async Task<IKey> GetPrivateKey() {
      foreach (var store in certStores) {
         var privateKey = await store.GetPrivateKey();
         if (privateKey != null) {
            return privateKey.Key;
         }
      }

      logger.LogTrace($"Did not find private key with in stores [{string.Join(",", certStores)}].");
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

      if (!result.Any()) {
         logger.LogWarning($"There are no saved challenges from stores {string.Join(",", values)}");
      }
      else {
         logger.LogTrace($"Challenges {result} from stores");
      }

      return result;
   }

   private async Task DeleteChallenges(IEnumerable<ChallengeInfo> challenges, IEnumerable<IChallengeStore> stores) {
      logger.LogTrace("Deleting challenges {challenges} through stores.", challenges);
      var tasks = stores.Select(store => store.Delete(challenges));
      await Task.WhenAll(tasks);
   }
}