// using Common.Accounts.Abstractions;
// using Common.EventSourcing;
// using Domain.Queries.Accounts;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
//
// public class TradeAllocatedEvtHandler : IEventHandler<TradeAllocatedEvt>
// {
//     private readonly IQueryBus _queryBus;
//
//     public TradeAllocatedEvtHandler(IQueryBus queryBus)
//     {
//         _queryBus = queryBus;
//     }
//
//     public void Handle(TradeAllocatedEvt evt)
//     {
//         var account = _queryBus.Query<GetAccount, IAccount>(new(evt.AccountId));
//         account.ProcessTrade(evt.Trade, evt.ProcessingDt);
//     }
// }