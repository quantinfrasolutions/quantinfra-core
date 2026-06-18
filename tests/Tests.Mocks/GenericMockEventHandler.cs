using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Tests.Mocks;

public class GenericMockEventHandler : IEventHandler
{
    public List<IEvent> Events = new();
    
    public void Handle(IEvent e) => Events.Add(e);
}

public class GenericMockEventHandler<T> : IEventHandler<T> where T : IEvent
{
    public List<T> Events = new();

    public void Handle(T evt) => Events.Add(evt);
}

public class MockProjectionHandler<T> : IProjectionWriter<T> where T : IProjectionUpdatedEvent
{
    public List<T> Projections = new();

    public void Write(T evt) => Projections.Add(evt);
}