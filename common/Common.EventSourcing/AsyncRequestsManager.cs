using System;
using System.Collections.Generic;

namespace QuantInfra.Common.EventSourcing;

public class AsyncRequestsManager<TKey>
{
    private readonly Dictionary<TKey, Guid> _requests = new();
    private readonly Dictionary<Guid, TKey> _reverseRequests = new();
    
    public bool TryCreateRequest(TKey id, out Guid requestId)
    {
        if (_requests.TryGetValue(id, out requestId)) return false;
        requestId = Guid.NewGuid();
        _requests[id] = requestId;
        _reverseRequests.Add(requestId, id);
        return true;
    }
    
    public bool RemoveRequest(Guid requestId)
    {
        if (_reverseRequests.Remove(requestId, out var id))
        {
            _requests.Remove(id);
            return true;
        }

        return false;
    }
}