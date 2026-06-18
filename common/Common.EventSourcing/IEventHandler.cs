namespace QuantInfra.Common.EventSourcing;

public interface IEventHandler
{
    void Handle(IEvent e);
}

public interface IEventHandler<T> where T : IEvent
{
    void Handle(T evt);
}
