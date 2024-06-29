namespace SparrowCert.Certes;

public class PfxCertificate(byte[] bytes) {
   public byte[] Bytes { get; } = bytes;
}