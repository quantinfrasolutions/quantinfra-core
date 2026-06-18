namespace QuantInfra.Common.EventSourcing
{
    public class Processor
    {
        private readonly IEventBus _eventBus;
        private readonly IQueryBus _queryBus;

        public Processor(IEventIdProvider eventIdProvider, IEventBus eventBus, IQueryBus queryBus)
        {
            EventIdProvider = eventIdProvider;
            _eventBus = eventBus;
            _queryBus = queryBus;
        }
        
        protected IEventIdProvider EventIdProvider { get; }
        
        protected void Emit<T>(T e) where T : IEvent =>
            _eventBus.Emit(e);

        protected void RegisterProjectionUpdate<T>(T e) where T : IProjectionUpdatedEvent =>
            _eventBus.RegisterProjectionUpdate(e);

        protected TResult Query<TQuery, TResult>(TQuery query) where TQuery : class, IQuery<TResult> =>
            _queryBus.Query<TQuery, TResult>(query);
    }
}

