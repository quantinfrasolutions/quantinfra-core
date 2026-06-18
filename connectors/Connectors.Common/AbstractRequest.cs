namespace QuantInfra.Connectors.Common;

public abstract class AbstractRequest<TId>
{
    public AbstractRequest(TId reqId)
    {
        ReqId = reqId;
    }
    
    public TId ReqId { get; }
    public abstract void FailRequest(string reason);
    public abstract void FailRequest(Exception exception);
}