namespace QuantInfra.Common.EventSourcing;

public interface IProcessor
{
    // List<IEvent> UncommittedEvents { get; }
    // void Commit();

    void Emit<T>(T e) where T : IEvent;
    TResult Query<TQuery, TResult>(TQuery query) where TQuery : class, IQuery<TResult>;
}