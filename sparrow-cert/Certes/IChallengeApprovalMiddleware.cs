using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SparrowCert.Certes;

public interface IChallengeApprovalMiddleware {
   Task InvokeAsync(HttpContext context);
}