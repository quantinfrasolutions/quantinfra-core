using System;

namespace QuantInfra.Common.EventSourcing;

public class MissingVersionException : Exception
{
    public MissingVersionException() : base() { }
    
    public MissingVersionException(string message) : base(message) { }
}