using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace QuantInfra.Common.EventSourcing;

public class InMemoryBus : IEventBus, ICommandBus, IQueryBus
{
    private readonly IServiceProvider _serviceProvider;

    private bool _genericEventHadlersCached, _genericProjectionWritersCached;
    private readonly List<IEventHandler> _genericEventHandlers = new();
    private readonly List<IProjectionWriter> _genericProjectionWriters = new();
    private readonly Dictionary<Type, List<object>> _typedEventHandlers = new();
    private readonly Dictionary<Type, object?> _typedExternalEventHandlers = new();
    private readonly Dictionary<Type, object?> _typedProjectionWriters = new();
    private readonly Dictionary<Type, object> _typedCommandHandlers = new();
    private readonly Dictionary<Type, Dictionary<Type, object>> _typedQueryHandlers = new();
    private readonly Dictionary<Type, Dictionary<Type, object?>> _typedAsyncQueryHandlers = new();
    
    private bool _universalQueryHandlerCached = false;
    private IAsyncQueryHandler? _universalAsyncQueryHandler = null;
    
    
    
    public InMemoryBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    public void Emit<T>(T e) where T : IEvent
    {
        if (!_genericEventHadlersCached)
        {
            _genericEventHandlers.AddRange(_serviceProvider.GetService<IEnumerable<IEventHandler>>() ?? Enumerable.Empty<IEventHandler>());
            _genericEventHadlersCached = true;
        }
        foreach (var h in _genericEventHandlers)
        {
            h.Handle(e);
        }
        
        var type = typeof(T);
        if (!_typedEventHandlers.ContainsKey(type))
        {
            _typedEventHandlers.Add(type, 
                _serviceProvider.GetService<IEnumerable<IEventHandler<T>>>()?.Select(h => (object)h).ToList()
                ?? new()
            );
        }
        foreach (var h in _typedEventHandlers[type])
        {
            ((IEventHandler<T>)h).Handle(e);
        }
    }
    
    public void EmitAnonymousEvent(IEvent e)
    {
        var type = e.GetType();
        var mi = GetType().GetMethod(nameof(Emit));
        var fooRef = mi!.MakeGenericMethod(type);
        fooRef.Invoke(this, new[] { e });
    }

    public void ApplyExternalEvent<T>(T e) where T : IEvent
    {
        var type = typeof(T);
        if (!_typedExternalEventHandlers.ContainsKey(type))
        {
            _typedExternalEventHandlers.Add(type, _serviceProvider.GetService<IExternalEventHandler<T>>());
        }
        var handler = (IExternalEventHandler<T>?)_typedExternalEventHandlers[type];        
        handler?.Apply(e);
    }

    public void ApplyAnonymousExternalEvent(IEvent e)
    {
        var type = e.GetType();
        var mi = GetType().GetMethod(nameof(ApplyExternalEvent));
        var fooRef = mi.MakeGenericMethod(type);
        fooRef.Invoke(this, new[] { e });
    }

    public void RegisterProjectionUpdate<T>(T e) where T : IProjectionUpdatedEvent
    {
        if (!_genericProjectionWritersCached)
        {
            _genericProjectionWriters.AddRange(_serviceProvider.GetService<IEnumerable<IProjectionWriter>>() 
                ?? Enumerable.Empty<IProjectionWriter>());
            _genericProjectionWritersCached = true;
        }

        foreach (var h in _genericProjectionWriters)
        {
            h.Write(e);
        }
        
        var type = typeof(T);
        if (!_typedProjectionWriters.ContainsKey(type))
        {
            _typedProjectionWriters.Add(type, _serviceProvider.GetService<IProjectionWriter<T>>());
        }
        var handler = (IProjectionWriter<T>?)_typedProjectionWriters[type];
        handler?.Write(e);
    }

    public void HandleAsyncQueryResponse<TRequest, TResult>(AsyncQueryResponse<TRequest, TResult> response)
    {
        var handler = _serviceProvider.GetService<IAsyncQueryResponseHandler<TRequest, TResult>>();
        handler?.Handle(response);
    }

    public void HandleAnonymousAsyncQueryResult(AsyncQueryResponse q)
    {
        var type = q.GetType().GetGenericArguments();
        var mi = GetType().GetMethod(nameof(HandleAsyncQueryResponse));
        var fooRef = mi.MakeGenericMethod(type[0], type[1]);
        fooRef.Invoke(this, new[] { q });
    }


    public void SendCommand<T>(T command) where T : ICommand
    {
        var type = typeof(T);
        if (!_typedCommandHandlers.ContainsKey(type))
        {
            _typedCommandHandlers.Add(type, _serviceProvider.GetService<ICommandHandler<T>>()
                ?? throw new InvalidOperationException($"No handlers found for command {type.Name}"));
        }
        ((ICommandHandler<T>)_typedCommandHandlers[type]).Handle(command);
    }

    public void SendAnonymousCommand(object command)
    {
        var type = command.GetType();
        var mi = GetType().GetMethod(nameof(SendCommand));
        var fooRef = mi.MakeGenericMethod(type);
        fooRef.Invoke(this, new[] { command });
    }


    public TResult Query<TRequest, TResult>(TRequest request) where TRequest : IQuery<TResult>
    {
        var tReq = typeof(TRequest);
        var tRes = typeof(TResult);

        if (!_typedQueryHandlers.TryGetValue(tReq, out var handlers) || !handlers.TryGetValue(tRes, out var handler))
        {
             handler = _serviceProvider.GetService<IQueryHandler<TRequest, TResult>>()
                ?? throw new NotSupportedException($"No handlers found for query {request.GetType().Name}");
            _typedQueryHandlers.TryAdd(tReq, new());
            _typedQueryHandlers[tReq].Add(tRes, handler);
        }
        return ((IQueryHandler<TRequest, TResult>)handler).Handle(request);
    }


    public void SendAsyncQuery<TRequest, TResult>(TRequest request) where TRequest : class, IAsyncQuery<TResult>
    {
        var tReq = typeof(TRequest);
        var tRes = typeof(TResult);

        if (!_typedAsyncQueryHandlers.TryGetValue(tReq, out var handlers) || !handlers.TryGetValue(tRes, out var handler))
        {
            handler = _serviceProvider.GetService<IAsyncQueryHandler<TRequest, TResult>>();
            _typedAsyncQueryHandlers.TryAdd(tReq, new());
            _typedAsyncQueryHandlers[tReq].Add(tRes, handler);
        }

        if (handler is not null)
        {
            ((IAsyncQueryHandler<TRequest, TResult>)handler).Handle(request);
            return;
        }
        
        if (!_universalQueryHandlerCached)
        {
            _universalAsyncQueryHandler = _serviceProvider.GetService<IAsyncQueryHandler>()
                ?? throw new NotSupportedException($"No handlers found for query {request.GetType().Name} and no universal handler was registered");
            _universalQueryHandlerCached = true;
        }
        _universalAsyncQueryHandler!.Handle(request);
    }
}