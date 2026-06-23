using Disruptor;
using Microsoft.Extensions.Logging;

namespace QuantInfra.Common.ServiceBase;

public sealed class DisruptorExceptionHandler<T>(IComponentExceptionHandler handler, ILogger<DisruptorExceptionHandler<T>> logger) : IExceptionHandler<T> where T : class
{
    public void HandleEventException(Exception ex, long sequence, T evt)
    {
        logger.LogCritical(ex, $"Unhandled exception in Disruptor handler. seq={sequence}, evt={evt}");
        handler.Raise(ex);
    }

    public void HandleOnTimeoutException(Exception ex, long sequence)
    {
        throw new NotImplementedException();
    }

    public void HandleEventException(Exception ex, long sequence, EventBatch<T> batch)
    {
        throw new NotImplementedException();
    }

    public void HandleOnStartException(Exception ex)
    {
        logger.LogCritical(ex, "Unhandled exception during Disruptor OnStart");
        handler.Raise(ex);
    }

    public void HandleOnShutdownException(Exception ex)
    {
        logger.LogCritical(ex, "Unhandled exception during Disruptor OnShutdown");
        handler.Raise(ex);
    }
}