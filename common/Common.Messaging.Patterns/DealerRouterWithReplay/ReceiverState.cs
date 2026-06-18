using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Common.Utils.Collections;

namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public class ReceiverState
{
    private readonly Dictionary<string, UpstreamSession> _sessions;
    
    public ReceiverState()
    {
        _sessions = new();
    }

    [JsonInclude]
    public IReadOnlyDictionary<string, UpstreamSession> Sessions
    {
        get => _sessions;
        init => _sessions = value.CopyAsDictionary();
    }
    
    public void SetSession(string senderCompId, long sessionId, long sequenceNumber)
    {
        if (Sessions.TryGetValue(senderCompId, out var session) && session.SessionId == sessionId)
        {
            session.SequenceNumber = sequenceNumber;
        }
        else
        {
            SetSession(senderCompId, new UpstreamSession(sessionId, sequenceNumber));
        }
    }

    public void SetSession(string senderCompId, UpstreamSession session) => _sessions[senderCompId] = session;

    public ReceiverState? Copy()
    {
        return new ReceiverState
        {
            Sessions = Sessions.ToDictionary(
                kv => kv.Key,
                kv => new UpstreamSession(kv.Value)
            ),
        };
    }

    public override string ToString()
    {
        return $"{nameof(Sessions)}: {string.Join(',', Sessions.Select(kv => $"{kv.Key}={kv.Value.SessionId}/{kv.Value.SequenceNumber}"))}";
    }
}

public interface IReceiverStateProvider
{
    ReceiverState GetReceiverState();
    void UpdateState(string senderCompId, long sessionId, long sequenceNumber);
}