namespace QuantInfra.Connectors.Common;

public sealed class Request<TId, TResult> : AbstractRequest<TId>
{
    public Request(TId reqId) : base(reqId)
    {
        Tcs = new();
    }
    
    public TaskCompletionSource<TResult> Tcs { get; }

    public override void FailRequest(string reason) =>
        FailRequest(new Exception(reason));

    public override void FailRequest(Exception exception) =>
        Tcs.SetException(exception);
}