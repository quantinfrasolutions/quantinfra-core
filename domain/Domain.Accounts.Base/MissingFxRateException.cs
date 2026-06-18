using System;

namespace QuantInfra.Domain.Accounts.Base;

public class MissingFxRateException : InvalidOperationException
{
    public MissingFxRateException()
    {
    }

    public MissingFxRateException(string message) : base(message)
    {
    }
}