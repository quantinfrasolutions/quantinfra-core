namespace QuantInfra.Common.EventSourcing;

public interface IExternalEventHandler<in TEvent> where TEvent : IEvent
{
    void Apply(TEvent e);
}