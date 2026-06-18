using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace QuantInfra.Common.EventSourcing;

public abstract class Aggregate
{
    private IEventBus _eventBus;
    protected ILogger Logger { get; private set; }
    
    protected Aggregate(long version)
    {
        Version = version;
    }
    
    [JsonPropertyName("version")] public virtual long Version { get; private set; }
    
    protected bool Apply(IAggregateEvent e)
    {
        var expectedVersion = Version + 1;
        if (e.Version > expectedVersion)
            throw new MissingVersionException($"Event version mismatch: expected {Version}, received {e.Version}");
        if (e.Version < expectedVersion) return false;

        Version = expectedVersion;
        return true;
    }
    
    public long GetNextVersion() => Version + 1;
    
    protected void Emit<T>(T? e) where T : IEvent
    {
        if (e is not null) _eventBus.Emit(e);
    }

    protected void RegisterProjectionUpdate<T>(T? e) where T : IProjectionUpdatedEvent
    {
        if (e is not null) _eventBus.RegisterProjectionUpdate(e);
    }

    protected void Initialize(IEventBus eventBus, ILogger logger)
    {
        _eventBus = eventBus;
        Logger = logger;
    }
}