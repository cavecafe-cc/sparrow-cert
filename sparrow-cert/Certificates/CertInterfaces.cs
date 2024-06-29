using Certes;

namespace SparrowCert.Certificates;

/// <summary>
/// A certificate which can be saved as a stream of bytes
/// </summary>
public interface IStorableCert : ICert {
   public byte[] RawData { get; }
}

/// <summary>
/// A certificate which can return an IKey
/// </summary>
public interface IKeyCert {
   IKey Key { get; }
}