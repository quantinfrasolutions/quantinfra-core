// using Common.Accounts.Abstractions;
// using Common.EventSourcing;
// using Domain.Queries.Accounts;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
//
// /// <summary>
// /// These events are issued by broker accounts when on order from an external account (ESA or SSA) is updated
// /// </summary>
// public sealed class ExternalAccountExecutionReportEvtHandler : IEventHandler<ExternalAccountExecutionReportEvt>
// {
//     private readonly IQueryBus _queryBus;
//
//     public ExternalAccountExecutionReportEvtHandler(IQueryBus queryBus)
//     {
//         _queryBus = queryBus;
//     }
//
//     public void Handle(ExternalAccountExecutionReportEvt evt)
//     {
//         var account = _queryBus.Query<GetAccount, IAccount>(new(evt.AccountId));
//         account.ProcessExecutionReport(evt.ExecutionReport, evt.ReferenceDt, evt.ProcessingDt);
//     }
// }