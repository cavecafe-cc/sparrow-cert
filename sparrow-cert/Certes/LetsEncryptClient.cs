using System;
using System.Linq;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;
using SparrowCert.Exceptions;
using SparrowCert.Store;

namespace SparrowCert.Certes;

public interface ILetsEncryptClient {
   Task<PlacedOrder> PlaceOrder(string[] domains);
   Task<PfxCertificate> FinalizeOrder(PlacedOrder placedOrder);
}

public class LetsEncryptClient(IAcmeContext acme, CertConfiguration options, ILogger logger) : ILetsEncryptClient {
   private string CertFriendlyName => (
      string.IsNullOrWhiteSpace(options.CertFriendlyName) ? options.Domains.First() : options.CertFriendlyName);

   public async Task<PlacedOrder> PlaceOrder(string[] domains) {
      logger.LogInformation($"Ordering LetsEncrypt certificate for domains {string.Join(',', domains)}.");
      var order = await acme.NewOrder(domains);
      var allAuthorizations = await order.Authorizations();
      var challengeContexts = await Task.WhenAll(allAuthorizations.Select(x => x.Http()));
      var nonNullChallengeContexts = challengeContexts.Where(c => c != null).ToArray();
      var challenges = nonNullChallengeContexts.Select(c => new ChallengeInfo {
         Token = c.Type == ChallengeTypes.Dns01 ? acme.AccountKey.DnsTxt(c.Token) : c.Token,
         Response = c.KeyAuthz,
         Domains = domains
      }).ToArray();

      logger.LogTrace($"LetsEncrypt placed order for domains {domains} with challenges {challenges}");

      return new PlacedOrder(challenges, order, nonNullChallengeContexts);
   }

   public async Task<PfxCertificate> FinalizeOrder(PlacedOrder placedOrder) {
      await ValidateChallenges(placedOrder.ChallengeContexts);
      var bytes = await AcquireCertificateBytesFromOrderAsync(placedOrder.Order);
      return new PfxCertificate(bytes);
   }

   private async Task ValidateChallenges(IChallengeContext[] contexts) {
      logger.LogInformation("Validating all pending order authorizations.");

      var responses = await ValidateChallengesAsync(contexts);
      var notEmptyResponses = responses.Where(challenge => challenge != null).ToArray();

      if (responses.Length > notEmptyResponses.Length)
         logger.LogWarning("Some challenge responses were null.");

      var exceptions = notEmptyResponses
         .Where(c => c.Status == ChallengeStatus.Invalid)
         .Select(c => new Exception($"{c.Error?.Type ?? "errorType null"}: {c.Error?.Detail ?? "null errorDetails"} (challenge type {c.Type ?? "null"})"))
         .ToArray();

      if (exceptions.Length > 0)
         throw new InvalidOrderException(
            "One or more LetsEncrypt orders were invalid. Make sure that LetsEncrypt can contact the domain you are trying to request an SSL certificate for, in order to verify it.",
            new AggregateException(exceptions));
   }

   private async Task<byte[]> AcquireCertificateBytesFromOrderAsync(IOrderContext order) {
      logger.LogInformation("Acquiring certificate through signing request.");

      var keyPair = KeyFactory.NewKey(options.KeyAlgorithm);
      var req = options.CertSigningRequest;
      var csrInfo = new CsrInfo {
         CountryName = req.CountryName,
         State = req.State,
         Locality = req.Locality,
         Organization = req.Organization,
         OrganizationUnit = req.OrganizationUnit,
         CommonName = req.CommonName
      };

      var certificateChain = await order.Generate(csrInfo, keyPair);
      var pfxBuilder = certificateChain.ToPfx(keyPair);
      pfxBuilder.FullChain = true;
      var pfxBytes = pfxBuilder.Build(CertFriendlyName, options.CertPwd);
      logger.LogInformation("Certificate acquired.");
      return pfxBytes;
   }

   private static async Task<Challenge[]> ValidateChallengesAsync(IChallengeContext[] contexts) {
      var challenges = await Task.WhenAll(contexts.Select(context => context.Validate()));

      while (true) {
         var allValid = challenges.All(challenge => challenge.Status == ChallengeStatus.Valid);
         var anyInvalid = challenges.Any(challenge => challenge.Status == ChallengeStatus.Invalid);

         if (allValid || anyInvalid)
            break;

         await Task.Delay(1000);
         challenges = await Task.WhenAll(contexts.Select(context => context.Resource()));
      }

      return challenges;
   }
}