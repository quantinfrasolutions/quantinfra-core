// using Common.EventSourcing;
// using Domain.Queries.Accounts;
// using NodaTime;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
//
// /// <summary>
// /// Events are issued when an execution report is received from an external platform
// /// </summary>
// public sealed class ExternalExecutionReportReceivedEvtHandler : IEventHandler<ExternalExecutionReportReceivedEvt>
// {
//     private readonly IQueryBus _queryBus;
//     private readonly IClock _clock;
//
//     public ExternalExecutionReportReceivedEvtHandler(IQueryBus queryBus, IClock clock)
//     {
//         _queryBus = queryBus;
//         _clock = clock;
//     }
//
//     public void Handle(ExternalExecutionReportReceivedEvt evt)
//     {
//         var er = evt.ExecutionReport;
//         var brokerAccount = _queryBus.Query<GetBrokerAccount, IBrokerAccount>(new(evt.AccountId));
//         brokerAccount.OnExternalExecutionReport(er, _clock.GetCurrentInstant());
//     }
// }