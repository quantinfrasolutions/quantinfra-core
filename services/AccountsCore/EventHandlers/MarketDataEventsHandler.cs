using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.VirtualExecution;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore.EventHandlers;

public class MarketDataEventsHandler :
    IEventHandler<ContractLastPriceUpdatedEvt>
{
    private readonly AccountServiceState _state;
    private readonly VirtualExecutor _ve;
    private readonly IClock _clock;
    private readonly IQueryBus _queryBus;
    private readonly IEventBus _eventBus;

    public MarketDataEventsHandler(AccountServiceState state, VirtualExecutor ve, IClock clock, IEventBus eventBus, IQueryBus queryBus)
    {
        _state = state;
        _ve = ve;
        _clock = clock;
        _eventBus = eventBus;
        _queryBus = queryBus;
    }
    
    public void Handle(ContractLastPriceUpdatedEvt evt)
    {
        _state.LastMarketDataEvtProcessingTs = _clock.GetCurrentInstant();
        _ve.CheckOrders(evt.ContractId, evt.Price, evt.TradingSessionId, evt.ReferenceDt,
            _clock.GetCurrentInstant(), _queryBus, _eventBus, 
            StopOrdersExecution.TriggerPrice
        );
    }
}

