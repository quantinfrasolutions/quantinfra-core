using Microsoft.Extensions.Logging;

namespace QuantInfra.Common.ServiceBase;

public sealed class FailFastExceptionHandler(ILogger<FailFastExceptionHandler> logger) : IComponentExceptionHandler
{
    public void Raise(Exception ex)
    {
        Environment.FailFast(
            $"Unhandled exception",
            ex);
    }
}