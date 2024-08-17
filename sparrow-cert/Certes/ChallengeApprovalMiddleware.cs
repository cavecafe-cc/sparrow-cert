using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SparrowCert.Store;

namespace SparrowCert.Certes;

public class ChallengeApprovalMiddleware(
   RequestDelegate next,
   IStore store) : IChallengeApprovalMiddleware {
   private const string tag = nameof(ChallengeApprovalMiddleware);
   private const string AcmeChallengePathPrefix = "/.well-known/acme-challenge";
   private static readonly PathString AcmeChallengePrefixSegments = new(AcmeChallengePathPrefix);

   public Task InvokeAsync(HttpContext context) {
      if (context.Request.Path.StartsWithSegments(AcmeChallengePrefixSegments)) {
         return ProcessAcmeChallenge(context);
      }

      return next(context);
   }

   private async Task ProcessAcmeChallenge(HttpContext context) {
      var path = context.Request.Path.ToString();

      Log.Info(tag, $"Challenge invoked: {path} by {context.Connection.RemoteIpAddress}");
      var requestedToken = path[$"{AcmeChallengePathPrefix}/".Length..];
      var allChallenges = await store.GetChallenges();
      var matchingChallenge = allChallenges.FirstOrDefault(x => x.Token == requestedToken);
      if (matchingChallenge == null) {
         Log.Info(tag, $"The given challenge did not match {path} among {allChallenges}");
         await next(context);
         return;
      }

      // token response is always in ASCII so char count would be equal to byte count here
      context.Response.ContentLength = matchingChallenge.Response.Length;
      context.Response.ContentType = "application/octet-stream";
      await context.Response.WriteAsync(
         text: matchingChallenge.Response,
         cancellationToken: context.RequestAborted);
   }
}