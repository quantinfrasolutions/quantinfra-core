namespace QuantInfra.Common.EventSourcing;

public abstract class AsyncQueryBus
{
    private readonly IQueryBus _queryBus;

    protected AsyncQueryBus(IQueryBus queryBus)
    {
        _queryBus = queryBus;
    }
    
    public void HandleAsyncQuery<TRequest, TResult>(TRequest request) where TRequest : class, IAsyncQuery<TResult>
    {
        var res = _queryBus.Query<TRequest, TResult>(request);
        var useMulticast = request is IAsyncQueryWithMulticast<TResult> { UseMulticast: true };
        SendAsyncQueryResponse<TRequest, TResult>(new(request.RequestId, res, useMulticast));
    }

    public void HandleAnonymousAsyncQuery(IAsyncQuery q)
    {
        var queryType = q.GetType();
        var resultType = queryType.GetInterfaces()[0].GetGenericArguments()[0];
        var mi = GetType().GetMethod(nameof(HandleAsyncQuery));
        var fooRef = mi!.MakeGenericMethod(queryType, resultType);
        fooRef.Invoke(this, new[] { q });
    }

    public abstract void SendAsyncQueryResponse<TRequest, TResult>(AsyncQueryResponse<TRequest, TResult> response);
}