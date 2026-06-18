using System.Text.Json.Serialization;

namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public class UpstreamSession
{
    [JsonConstructor] public UpstreamSession() { }
    
    public UpstreamSession(long sessionId, long sequenceNumber)
    {
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
    }

    public UpstreamSession(UpstreamSession session) : this(session.SessionId, session.SequenceNumber)
    {
        RequestedFillGaps = session.RequestedFillGaps;
    }
    
    public long SessionId { get; init; }
    public long SequenceNumber { get; set; }
    public long? RequestedFillGaps { get; set; }
}