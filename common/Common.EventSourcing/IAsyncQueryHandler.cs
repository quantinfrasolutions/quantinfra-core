namespace QuantInfra.Common.EventSourcing;

public interface IAsyncQueryHandler
{
    void Handle(IAsyncQuery query);
}

public interface IAsyncQueryHandler<in TRequest, out TResult>
{
    void Handle(TRequest query);
    // Task<TResult> HandleAsync(TRequest query);
}