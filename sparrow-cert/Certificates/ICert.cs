using System;

namespace SparrowCert.Certificates;

/// <summary>
/// The most generic form of certificate, metadata provision only
/// </summary>
public interface ICert {
   public DateTime NotAfter { get; }
   public DateTime NotBefore { get; }
   string Thumbprint { get; }
}