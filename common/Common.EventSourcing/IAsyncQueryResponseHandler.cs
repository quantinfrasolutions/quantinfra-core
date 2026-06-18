namespace QuantInfra.Common.EventSourcing;

public interface IAsyncQueryResponseHandler<TRequest, TResult>
{
    void Handle(AsyncQueryResponse<TRequest, TResult> response);
}