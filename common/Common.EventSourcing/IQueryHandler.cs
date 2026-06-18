namespace QuantInfra.Common.EventSourcing;

public interface IQueryHandler<in TRequest, TResult>
{
    TResult Handle(TRequest query);
    // Task<TResult> HandleAsync(TRequest query);
}