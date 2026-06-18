using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Common;

public class SubscriptionsManager<TRequest, TSubscription>
    where TSubscription : ISubscription
{
    private int _reqId;
    private readonly Dictionary<int, AbstractRequest<int>> _requests = new();
    private readonly Dictionary<int, TSubscription> _subscriptions = new();
    private readonly object _dictLock = new();
    
    private readonly Func<TRequest, int, TSubscription> _subscriptionFactory;
    private readonly Func<TSubscription, Task> _subscribeFunc;
    private readonly int _timeoutMs;

    public IReadOnlyDictionary<int, TSubscription> Subscriptions => _subscriptions;
    public IReadOnlyDictionary<int, AbstractRequest<int>> Requests => _requests;
    
    public SubscriptionsManager(
        Func<TRequest, int, TSubscription> subscriptionFactory,
        Func<TSubscription, Task> subscribeFunc,
        int timeoutMs = 10000
    )
    {
        _subscriptionFactory = subscriptionFactory;
        _subscribeFunc = subscribeFunc;
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Tries to create a new market data subscription using a request
    /// </summary>
    public Task<TSubscription> Subscribe(TRequest request, Predicate<TSubscription>? considerSubscriptionSuccessfulAfterTimeout = null)
    {
        var (reqId, _) = CreateRequest<TSubscription>();
        var subscription = _subscriptionFactory(request, reqId);
        return Subscribe(subscription, considerSubscriptionSuccessfulAfterTimeout);
    }

    /// <summary>
    /// Tries to create a market data subscription
    /// </summary>
    public Task<TSubscription> Subscribe(TSubscription subscription, Predicate<TSubscription>? considerSubscriptionSuccessfulAfterTimeout = null, 
        Func<TSubscription, Task>? subscribeFuncOverride = null)
    {
        var subscribeFunc = subscribeFuncOverride ?? _subscribeFunc;
        
        lock (_dictLock)
        {
            _subscriptions.Add(subscription.SubscriptionId, subscription);
        }
        
        if (!_requests.ContainsKey(subscription.SubscriptionId)) CreateRequest<TSubscription>(subscription.SubscriptionId);

        var request = (Request<int, TSubscription>)_requests[subscription.SubscriptionId];
        
        var task = Task.Run(async () =>
        {
            await subscribeFunc(subscription);
            return await request.Tcs.Task;
        });

        Task.Run(async () =>
        {
            // Timeout
            if (await Task.WhenAny(task, Task.Delay(_timeoutMs)) != task)
            {
                if (considerSubscriptionSuccessfulAfterTimeout?.Invoke(subscription) != true)
                {
                    FailSubscription(subscription.SubscriptionId, "Subscription timed out");
                }
                else
                {
                    ConfirmSubscription(subscription.SubscriptionId);
                }
            }
        });

        return task;
    }

    /// <summary>
    /// Sends an arbitrary request and returns an awaitable task with the result 
    /// </summary>
    public Task<TResult> SendRequest<TResult>(Func<int, Task> subscribeMethod, int timeoutMilliseconds = 10000,
        Func<int, Task>? cleanupMethod = null)
    {
        var (reqId, request) = CreateRequest<TResult>();
        
        var task = Task.Run(async () =>
        {
            await subscribeMethod(reqId);
            return await request.Tcs.Task;
        });

        Task.Run(async () =>
        {
            if (await Task.WhenAny(task, Task.Delay(_timeoutMs)) != task)
            {
                await FailRequest(reqId, "Request timed out", cleanupMethod);
            }
        });

        return task;
    }

    private (int, Request<int, TResult>) CreateRequest<TResult>(int id = 0)
    {
        lock (_dictLock)
        {
            if (id == 0)
            {


                if (_reqId == int.MaxValue)
                {
                    _reqId = 0;
                }

                do
                {
                    _reqId++;
                } while (_requests.ContainsKey(_reqId) || _subscriptions.ContainsKey(_reqId));

                id = _reqId;
            }

            var request = new Request<int, TResult>(id);
            _requests.Add(id, request);
            return (id, request);
        }
    }

    public void ConfirmSubscription(int requestId)
    {
        CompleteRequest(requestId, _subscriptions[requestId]);
    }

    public void FailSubscription(int requestId, string message)
    {
        var request = RemoveRequest(requestId);
        if (request != null)
        {
            _subscriptions.Remove(requestId);
            request.FailRequest(message);
        }
    }

    public void CompleteRequest<TResult>(int requestId, TResult result)
    {
        var request = RemoveRequest<TResult>(requestId);
        request?.Tcs.SetResult(result);
    }

    public void FailRequest(int requestId, string message)
    {
        var request = RemoveRequest(requestId);
        request?.FailRequest(message);
    }
    
    public async Task FailRequest(int requestId, string message, Func<int, Task>? cleanupMethod)
    {
        var request = RemoveRequest(requestId);
        if (cleanupMethod != null) await cleanupMethod(requestId);
        request?.FailRequest(message);
    }

    private AbstractRequest<int>? RemoveRequest(int requestId)
    {
        lock (_dictLock)
        {
            _requests.Remove(requestId, out var request);
            return request;
        }
    }
    
    private Request<int, TResult>? RemoveRequest<TResult>(int requestId)
    {
        lock (_dictLock)
        {
            _requests.Remove(requestId, out var request);
            return (Request<int, TResult>)request;
        }
    }
}