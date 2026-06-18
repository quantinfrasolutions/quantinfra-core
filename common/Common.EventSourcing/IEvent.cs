using NodaTime;

namespace QuantInfra.Common.EventSourcing;

public interface IEvent
{
	long EventId { get; }
	Instant Timestamp { get; }
}

public interface IAggregateEvent : IEvent
{
	long Version { get; init; }
}

public interface IProjectionUpdatedEvent
{
	long EventId { get; }
}