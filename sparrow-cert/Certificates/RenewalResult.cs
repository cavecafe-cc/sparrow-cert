namespace SparrowCert.Certificates;

public class RenewalResult(ICert cert, RenewalStatus status) {
   public ICert Cert { get; } = cert;

   public RenewalStatus Status { get; } = status;
}