// using Common.EventSourcing;
// using Domain.Events.ExternalAccounts;
// using Domain.Queries.Accounts;
// using NodaTime;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
//
// public class ExternalTradeReceivedEvtHandler : IEventHandler<ExternalTradeReceivedEvt>
// {
//     private readonly IQueryBus _queryBus;
//     private readonly IClock _clock;
//
//     public ExternalTradeReceivedEvtHandler(IQueryBus queryBus, IClock clock)
//     {
//         _queryBus = queryBus;
//         _clock = clock;
//     }
//
//     public void Handle(ExternalTradeReceivedEvt evt)
//     {
//         var trade = evt.Trade;
//         var account = _queryBus.Query<GetBrokerAccount, IBrokerAccount>(new(trade.AccountId));
//         account.OnExternalTrade(trade, _clock.GetCurrentInstant());
//     }
// }