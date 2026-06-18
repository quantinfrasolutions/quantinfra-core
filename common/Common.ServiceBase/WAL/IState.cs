namespace QuantInfra.Common.ServiceBase.WAL;

public interface IState<in T> where T : class, new()
{
    void Initialize(T state);
    
    long LastFinalizedEventId { get; }
    long LastFinalizedTimestamp { get; }
    void UpdateLastSentEventId(long eventId, long timestamp);
}