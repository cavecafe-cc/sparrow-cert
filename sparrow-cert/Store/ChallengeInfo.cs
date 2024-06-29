namespace SparrowCert.Store;

public class ChallengeInfo {
   public string Token { get; init; }
   public string Response { get; init; }
   public string[] Domains { get; set; }

   public override string ToString() {
      return $"Token: {Token}";
   }
}