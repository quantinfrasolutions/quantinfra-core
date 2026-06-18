// using Common.Accounts.Abstractions;
// using Common.EventSourcing;
// using Common.Trading.Orders;
// using Domain.Events.Accounts;
// using Domain.Queries.Accounts;
// using Microsoft.Extensions.Logging;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace Domain.Accounts.EventHandlers.BrokerAccounts;
//
// public sealed class BrokerAccountExecutionReportEvtHandler : IEventHandler<ExecutionReportEvt>
// {
//     private readonly IQueryBus _queryBus;
//     private readonly ILogger<BrokerAccountExecutionReportEvtHandler> _logger;
//
//     public BrokerAccountExecutionReportEvtHandler(IQueryBus queryBus, ILogger<BrokerAccountExecutionReportEvtHandler> logger)
//     {
//         _queryBus = queryBus;
//         _logger = logger;
//     }
//
//     public void Handle(ExecutionReportEvt evt)
//     {
//         #if !FAST
//         _logger.LogDebug($"Handle {evt}");
//         #endif
//         if (evt.AccountType == AccountType.BrokerAccount && evt.AccountId != evt.ExecutionReport.AccountId)
//         {
//             var er = evt.ExecutionReport;
//
//             if (er.ExecType == ExecType.PendingNew || er.ExecType == ExecType.PendingCancel ||
//                 er.ExecType == ExecType.PendingReplace) return;
//             
//             #if !FAST
//             _logger.LogInformation($"Handle ExecutionReportEvt ExecId={er.ExecId} ExecType={er.ExecType} AccountId={er.AccountId}");
//             #endif
//
//             var account = _queryBus.Query<GetAccount, IAccount>(new(er.AccountId));
//             account.ProcessExecutionReport(er, evt.ReferenceDt, evt.ProcessingDt);
//         }
//         // TODO: move to a separate handler
//         // else if (evt.AccountType == AccountType.VirtualAccount || evt.AccountType == AccountType.StrategySubAccount)
//         // {
//         //     IReadOnlyStrategiesRepository sr;
//         //     IStrategiesProvider sp;
//         //     var strategyId = sr.GetStrategyIdByAccountId(evt.AccountId);
//         //     if (strategyId.HasValue)
//         //     {
//         //         sp.GetStrategy(strategyId.Value).ProcessExecutionReport(evt.ExecutionReport, evt.ReferenceDt, _clock.GetCurrentInstant());
//         //     }
//         // }
//     }
// }