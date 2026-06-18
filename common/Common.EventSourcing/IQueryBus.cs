namespace QuantInfra.Common.EventSourcing;

public interface IQueryBus
{
    TResult Query<TRequest, TResult>(TRequest request) where TRequest : IQuery<TResult>;
    
    void SendAsyncQuery<TRequest, TResult>(TRequest request) where TRequest : class, IAsyncQuery<TResult>;
}