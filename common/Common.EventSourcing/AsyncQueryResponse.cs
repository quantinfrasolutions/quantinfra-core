using System;

namespace QuantInfra.Common.EventSourcing;

public class AsyncQueryResponse
{
    public AsyncQueryResponse(Guid requestId)
    {
        RequestId = requestId;
    }

    public Guid RequestId { get; }
}

public class AsyncQueryResponse<TRequest, TResult> : AsyncQueryResponse
{
    public AsyncQueryResponse(Guid requestId, TResult result, bool useMulticast = false) : base(requestId)
    {
        Result = result;
        UseMulticast = useMulticast;
    }
    
    
    public TResult Result { get; }
    public bool UseMulticast { get; }
}