namespace QuantInfra.Common.EventSourcing;

public interface IEventBus
{
    void Emit<T>(T e) where T : IEvent;
    void EmitAnonymousEvent(IEvent e);
    void ApplyExternalEvent<T>(T e) where T : IEvent;
    void ApplyAnonymousExternalEvent(IEvent e);
    void RegisterProjectionUpdate<T>(T @event) where T : IProjectionUpdatedEvent;
    void HandleAsyncQueryResponse<TRequest, TResult>(AsyncQueryResponse<TRequest, TResult> response);
    void HandleAnonymousAsyncQueryResult(AsyncQueryResponse q);
}