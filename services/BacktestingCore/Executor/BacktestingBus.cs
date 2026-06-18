// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Common.EventSourcing;
// using Common.Trading.Orders;
// using Domain.Commands.Accounts.AccountsService;
// using Domain.Queries.Accounts.AccountsService;
// using Microsoft.Extensions.DependencyInjection;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Strategies;
// using QuantInfra.Domain.Events.Accounts.AccountsService;
// using QuantInfra.Domain.Events.Accounts.Management;
// using QuantInfra.Domain.Events.Accounts.VirtualExecution;
// using QuantInfra.Domain.Events.Strategies.Management;
// using QuantInfra.Domain.Queries.StaticData;
// using QuantInfra.Domain.Queries.Strategies;
// using Contract = QuantInfra.Common.StaticData.Abstractions.Contract;
//
//
// namespace BacktestingCore.Executor;
//
// /// <summary>
// /// This class has the same functionality as the InMemoryBus, but tries to save some time by having all required handlers
// /// cached instead of being retrieved from the service provider on every call 
// /// </summary>
// internal class BacktestingBus : ICommandBus, IEventBus, IQueryBus
// {
//     private ICommandHandler<ProcessBalanceOperationCmd> _processBalanceOperation;
//     private ICommandHandler<RunEndOfDayCmd> _eodCmdHandler;
//     private IEventHandler<AccountCreatedEvt> _accountCreatedEvtHandler;
//     private IEventHandler<StrategyCreatedEvt> _strategyCreatedEvtHandler;
//     private List<IEventHandler<ExecutionReportEvt>> _erEvtHandlers;
//     private List<IEventHandler<TradeEvt>> _tradeEvtHandlers;
//     // private IEventHandler<PositionChangedEvt> _positionChangedEvtHandler;
//     // private IEventHandler<InvestmentChangedEvt> _investmentChangedEvtHandler;
//     private List<IEventHandler<SharePriceUpdatedEvt>> _sharePriceUpdatedEvtHanders;
//     private IEventHandler<VirtualOrderStatusChangedEvt> _virtualOrderStatusChangedEvtHandler;
//     private IQueryHandler<GetAccount, IAccount> _getAccountQueryHandler;
//     private IQueryHandler<GetStrategy, IStrategy> _getStrategyQueryHandler;
//     private IQueryHandler<GetStrategyRecord, Strategy> _getStrategyRecordQueryHandler;
//     // private IQueryHandler<GetStrategyIdByAccountId, Guid?> _getStrategyIdByAccountId;
//     private IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>> _getVirtualExecutorOrders;
//     private IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyList<Guid>> _getAccountIdsForEndOfDayQueryHandler;
//     // private IQueryHandler<GetContractIdsOfActivePositions, IReadOnlyList<long>> _getContractIdsOfActivePositionsQueryHandler;
//     private IQueryHandler<GetContract, Contract> _getContractQueryHandler;
//         
//     public void InitializeHandlers(IServiceProvider serviceProvider)
//     {
//         _accountCreatedEvtHandler = serviceProvider.GetRequiredService<IEventHandler<AccountCreatedEvt>>();
//         _strategyCreatedEvtHandler = serviceProvider.GetRequiredService<IEventHandler<StrategyCreatedEvt>>();
//         _processBalanceOperation = serviceProvider.GetService<ICommandHandler<ProcessBalanceOperationCmd>>();
//         if (_processBalanceOperation == null) throw new InvalidOperationException(nameof(_processBalanceOperation));
//         _eodCmdHandler = serviceProvider.GetService<ICommandHandler<RunEndOfDayCmd>>();
//         if (_eodCmdHandler == null) throw new InvalidOperationException(nameof(_eodCmdHandler));
//         _erEvtHandlers = serviceProvider.GetService<IEnumerable<IEventHandler<ExecutionReportEvt>>>()?.ToList();
//         // if (_erEvtHandlers == null || _erEvtHandlers.Count != 3) throw new InvalidOperationException(nameof(_erEvtHandlers));
//         _tradeEvtHandlers = serviceProvider.GetService<IEnumerable<IEventHandler<TradeEvt>>>()?.ToList();
//         // if (_tradeEvtHandlers == null || _tradeEvtHandlers.Count != 2) throw new InvalidOperationException(nameof(_tradeEvtHandlers));
//         // _positionChangedEvtHandler = serviceProvider.GetService<IEventHandler<PositionChangedEvt>>();
//         // if (_positionChangedEvtHandler == null) throw new InvalidOperationException(nameof(_positionChangedEvtHandler));
//         // _investmentChangedEvtHandler = serviceProvider.GetService<IEventHandler<InvestmentChangedEvt>>();
//         // if (_investmentChangedEvtHandler == null) throw new InvalidOperationException(nameof(_investmentChangedEvtHandler));
//         _sharePriceUpdatedEvtHanders = serviceProvider.GetRequiredService<IEnumerable<IEventHandler<SharePriceUpdatedEvt>>>()?.ToList();
//         // if (_sharePriceUpdatedEvtHanders == null || _sharePriceUpdatedEvtHanders.Count != 2) throw new InvalidOperationException(nameof(_sharePriceUpdatedEvtHanders));
//         _virtualOrderStatusChangedEvtHandler = serviceProvider.GetRequiredService<IEventHandler<VirtualOrderStatusChangedEvt>>();
//         _getAccountQueryHandler = serviceProvider.GetService<IQueryHandler<GetAccount, IAccount>>()!;
//         // if (_getAccountQueryHandler == null) throw new InvalidOperationException(nameof(_getAccountQueryHandler));
//         // _getStrategyQueryHandler = serviceProvider.GetService<IQueryHandler<GetStrategy, IStrategy>>()!;
//         // if (_getStrategyQueryHandler == null) throw new InvalidOperationException(nameof(_getStrategyQueryHandler));
//         _getStrategyRecordQueryHandler = serviceProvider.GetRequiredService<IQueryHandler<GetStrategyRecord, Strategy>>();
//         // _getStrategyIdByAccountId = serviceProvider.GetService<IQueryHandler<GetStrategyIdByAccountId, Guid?>>()!;
//         // if (_getStrategyIdByAccountId == null) throw new InvalidOperationException(nameof(_getStrategyIdByAccountId));
//         _getVirtualExecutorOrders = serviceProvider.GetService<IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>>()!;
//         if (_getVirtualExecutorOrders == null) throw new InvalidOperationException(nameof(_getVirtualExecutorOrders));
//         // _getAccountIdsForEndOfDayQueryHandler = serviceProvider.GetService<IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyList<Guid>>>();
//         // if (_getAccountIdsForEndOfDayQueryHandler == null) throw new InvalidOperationException(nameof(_getAccountIdsForEndOfDayQueryHandler));
//         // _getContractIdsOfActivePositionsQueryHandler = serviceProvider.GetService<IQueryHandler<GetContractIdsOfActivePositions, IReadOnlyList<long>>>();
//         // if (_getContractIdsOfActivePositionsQueryHandler == null) throw new InvalidOperationException(nameof(_getContractIdsOfActivePositionsQueryHandler));
//         // _getContractQueryHandler = serviceProvider.GetService<IQueryHandler<GetContract, Contract>>();
//         // if (_getContractQueryHandler == null) throw new InvalidOperationException(nameof(_getContractQueryHandler));
//     }
//     
//     public void SendCommand<T>(T command) where T : ICommand
//     {
//         throw new NotSupportedException();
//         // switch (command)
//         // {
//         //     case CreateStrategyCmd cs: _createStrategy.Handle(cs);
//         //         break;
//         //     case CreateAccountCmd ca: _createAccount.Handle(ca);
//         //         break;
//         //     case ProcessBalanceOperationCmd bo: _processBalanceOperation.Handle(bo);
//         //         break;
//         //     case EndOfDayCmd eod: _eodCmdHandler.Handle(eod);
//         //         break;
//         //     default:
//         //         throw new NotSupportedException($"{command.GetType()}");
//         // }
//     }
//
//     public void SendAnonymousCommand(object command)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task SendCommandAsync<T>(T command) where T : ICommand
//     {
//         throw new System.NotSupportedException();
//     }
//
//     public void Emit<T>(T e) where T : IEvent
//     {
//         switch (e)
//         {
//             case AccountCreatedEvt ac:
//                 _accountCreatedEvtHandler.Handle(ac);
//                 break;
//             case StrategyCreatedEvt sc:
//                 _strategyCreatedEvtHandler.Handle(sc);
//                 break;
//             case ExecutionReportEvt er:
//                 foreach (var handler in _erEvtHandlers) handler.Handle(er);
//                 break;
//             case TradeEvt t: foreach (var handler in _tradeEvtHandlers) handler.Handle(t);
//                 break;
//             // case PositionChangedEvt p: _positionChangedEvtHandler.Handle(p);
//             //     break;
//             // case InvestmentChangedEvt i: _investmentChangedEvtHandler.Handle(i);
//             //     break;
//             case SharePriceUpdatedEvt sp: foreach (var handler in _sharePriceUpdatedEvtHanders) handler.Handle(sp);
//                 break;
//             case VirtualOrderStatusChangedEvt vo: _virtualOrderStatusChangedEvtHandler.Handle(vo);
//                 break;
//         }
//     }
//
//     public void EmitAnonymousEvent(IEvent e)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void ApplyExternalEvent<T>(T e) where T : IEvent => throw new NotSupportedException();
//     public void ApplyAnonymousExternalEvent(IEvent e) => throw new NotImplementedException();
//     public void RegisterProjectionUpdate<T>(T @event) where T : IProjectionUpdatedEvent
//     {
//         throw new NotImplementedException();
//     }
//
//     public void HandleAsyncQueryResponse<TRequest, TResult>(AsyncQueryResponse<TRequest, TResult> response)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void HandleAnonymousAsyncQueryResult(AsyncQueryResponse q)
//     {
//         throw new NotImplementedException();
//     }
//     
//
//     public void SendAsyncQuery<TRequest, TResult>(TRequest request) where TRequest : class, IAsyncQuery<TResult>
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<TResult> QueryAsync<TRequest, TResult>(TRequest request) where TRequest : class, IQuery<TResult>
//     {
//         throw new System.NotSupportedException();
//     }
//
//     TResult IQueryBus.Query<TRequest, TResult>(TRequest request)
//     {
//         switch (request)
//         {
//             case GetStrategyRecord getSr:
//                 return (TResult)_getStrategyRecordQueryHandler.Handle(getSr as TRequest);
//                 break;
//             // case GetAccount getAcc: return ((IQueryHandler<TRequest, TResult>)_getAccountQueryHandler).Handle(getAcc as TRequest);
//             //     break;
//             // case GetStrategy getStrategy: return ((IQueryHandler<TRequest, TResult>)_getStrategyQueryHandler).Handle(getStrategy as TRequest);
//             //     break;
//             // case GetStrategyIdByAccountId getStrategyIdByAccountId: return ((IQueryHandler<TRequest, TResult>)_getStrategyIdByAccountId).Handle(getStrategyIdByAccountId as TRequest);
//             //     break;
//             case GetActiveVirtualExecutorOrders getActiveVirtualExecutorOrders:
//                 return (TResult)_getVirtualExecutorOrders.Handle(getActiveVirtualExecutorOrders);
//                 break; 
//             // case GetAccountIdsForEndOfDay getAccountIdsForEndOfDay: return ((IQueryHandler<TRequest, TResult>)_getAccountIdsForEndOfDayQueryHandler).Handle(getAccountIdsForEndOfDay as TRequest);
//             //     break; 
//             // case GetContract getContract: return ((IQueryHandler<TRequest, TResult>)_getContractQueryHandler).Handle(getContract as TRequest);
//             //     break;
//             // case GetContractIdsOfActivePositions getContractIdsOfActivePositions: return ((IQueryHandler<TRequest, TResult>)_getContractIdsOfActivePositionsQueryHandler).Handle(getContractIdsOfActivePositions as TRequest);
//             //     break; 
//             default:
//                 throw new NotSupportedException($"{request.GetType()}");
//         }
//     }
// }
//
// internal static class Extensions
// {
//     public static IServiceCollection UseBacktestingBus(this IServiceCollection sc) => sc
//         .AddSingleton<BacktestingBus>()
//         .AddSingleton<ICommandBus>(sp => sp.GetService<BacktestingBus>())
//         .AddSingleton<IEventBus>(sp => sp.GetService<BacktestingBus>())
//         .AddSingleton<IQueryBus>(sp => sp.GetService<BacktestingBus>());
// }
//
