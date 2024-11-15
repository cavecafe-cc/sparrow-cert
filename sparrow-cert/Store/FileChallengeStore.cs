﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace SparrowCert.Store;

public class FileChallengeStore(string storePath, string filePrefix) : IChallengeStore {
   public async Task Delete(IEnumerable<ChallengeInfo> challenges) {
      var fromStored = await Load();
      var toStore = fromStored
         .Where(x =>
            challenges.All(y => y.Token != x.Token))
         .ToList();

      await Save(toStore);
   }

   public Task Save(IEnumerable<ChallengeInfo> challenges) {
      var json = challenges == null ? null : JsonSerializer.Serialize(challenges.ToArray());
      var bytes = json == null ? null : Encoding.UTF8.GetBytes(json);
      if (bytes == null)
         return Task.FromException(new ArgumentNullException(nameof(challenges)));

      lock (typeof(FileChallengeStore)) {
         File.WriteAllBytes(
            GetStorePath(),
            bytes);
      }

      return Task.CompletedTask;
   }

   public Task<IEnumerable<ChallengeInfo>> Load() {
      lock (typeof(FileChallengeStore)) {
         var challengePath = GetStorePath();
         if (!File.Exists(challengePath))
            return Task.FromResult<IEnumerable<ChallengeInfo>>(new List<ChallengeInfo>());

         var bytes = File.ReadAllBytes(challengePath);
         var json = Encoding.UTF8.GetString(bytes);
         var challenges = JsonSerializer.Deserialize<IEnumerable<ChallengeInfo>>(json);
         return Task.FromResult(challenges);
      }
   }

   private string GetStorePath() {
      return Path.Combine(storePath, filePrefix + "-challenges.json");
   }
}