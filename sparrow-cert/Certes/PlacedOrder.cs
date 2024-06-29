using Certes.Acme;
using SparrowCert.Store;

namespace SparrowCert.Certes;

public class PlacedOrder(
   ChallengeInfo[] challenges,
   IOrderContext order,
   IChallengeContext[] challengeContexts) {
   public ChallengeInfo[] Challenges { get; } = challenges;
   public IOrderContext Order { get; } = order;
   public IChallengeContext[] ChallengeContexts { get; } = challengeContexts;
}