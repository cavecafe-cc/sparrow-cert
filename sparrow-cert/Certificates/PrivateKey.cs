using System;
using System.Text;
using Certes;

namespace SparrowCert.Certificates;

/// <summary>
/// The type of certificate used to store a Let's Encrypt private key (for account) or a generated private
/// </summary>
public class PrivateKey : IStorableCert, IKeyCert {
   public PrivateKey(IKey key) {
      Key = key;
      var text = key.ToPem();
      RawData = Encoding.UTF8.GetBytes(text);
   }

   public PrivateKey(byte[] bytes) {
      RawData = bytes;
      var text = Encoding.UTF8.GetString(bytes);
      Key = KeyFactory.FromPem(text);
   }

   public DateTime NotAfter => throw new InvalidOperationException("No metadata available for not-after");
   public DateTime NotBefore => throw new InvalidOperationException("No metadata available for not-before");
   public string Thumbprint => throw new InvalidOperationException("No metadata available for thumbprint");

   public byte[] RawData { get; }
   public IKey Key { get; }
}