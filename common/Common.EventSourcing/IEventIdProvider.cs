namespace QuantInfra.Common.EventSourcing;

public interface IEventIdProvider
{
    long GetNextEventId();
}