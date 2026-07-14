namespace QuantInfra.Connectors.Common;

public class RequestsManager<TId> where TId : struct
{
    private readonly object _dictLock = new();
    private readonly Dictionary<TId, AbstractRequest<TId>> _requests = new();
    private readonly Func<TId> _newIdFactory;

    public RequestsManager(Func<TId> newIdFactory)
    {
        _newIdFactory = newIdFactory;
    }
    
    public IReadOnlyDictionary<TId, AbstractRequest<TId>> Requests => _requests;
    
    public Task<TResult> SendRequest<TResult>(Func<TId, Task> requestMethod, int timeoutMilliseconds = 100000000,
        Func<TId, Task>? cleanupMethod = null, TId? id = null)
    {
        var (reqId, request) = CreateRequest<TResult>(id);
        
        var task = Task.Run(async () =>
        {
            await requestMethod(reqId);
            return await request.Tcs.Task;
        });

        if (timeoutMilliseconds > 0)
        {
            Task.Run(async () =>
            {
                if (await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)) != task)
                {
                    await FailRequest(reqId, "Request timed out", cleanupMethod);
                }
            });
        }

        return task;
    }
    
    private (TId, Request<TId, TResult>) CreateRequest<TResult>(TId? id = null)
    {
        lock (_dictLock)
        {
            if (id == null)
            {
                do
                {
                    id = _newIdFactory();
                } while (_requests.ContainsKey(id.Value));
            }

            var request = new Request<TId, TResult>(id.Value);
            _requests.Add(id.Value, request);
            return (id.Value, request);
        }
    }
    
    public void CompleteRequest<TResult>(TId requestId, TResult result)
    {
        var request = RemoveRequest<TResult>(requestId);
        request?.Tcs.SetResult(result);
    }

    public void FailRequest(TId requestId, string message)
    {
        var request = RemoveRequest(requestId);
        request?.FailRequest(message);
    }
    
    public async Task FailRequest(TId requestId, string message, Func<TId, Task>? cleanupMethod)
    {
        var request = RemoveRequest(requestId);
        if (cleanupMethod != null) await cleanupMethod(requestId);
        request?.FailRequest(message);
    }

    private AbstractRequest<TId>? RemoveRequest(TId requestId)
    {
        lock (_dictLock)
        {
            _requests.Remove(requestId, out var request);
            return request;
        }
    }
    
    private Request<TId, TResult>? RemoveRequest<TResult>(TId requestId)
    {
        lock (_dictLock)
        {
            _requests.Remove(requestId, out var request);
            return (Request<TId, TResult>)request;
        }
    }
}