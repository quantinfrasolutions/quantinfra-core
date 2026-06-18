namespace QuantInfra.Common.EventSourcing;

public interface IProjectionWriter
{
    void Write(IProjectionUpdatedEvent e);
}

public interface IProjectionWriter<in TEvent> where TEvent : IProjectionUpdatedEvent
{
    void Write(TEvent evt);
}