using System;
using System.Runtime.Serialization;

namespace SparrowCert.Exceptions;

internal class InvalidOrderException : Exception {
   public InvalidOrderException() { }

   public InvalidOrderException(string message) : base(message) { }

   public InvalidOrderException(string message, Exception innerException) : base(message, innerException) { }
}