using Common.Trading;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.VirtualExecution;

public class VirtualExecutor : IConfigurableLoggingModule
{
    private readonly IEventIdProvider _eventIdProvider;
    private readonly IExecIdProvider _execIdProvider;
    private readonly ILogger<VirtualExecutor> _logger;

    /// <summary>
    /// ContractId.AccountId.OrderId => OrderStatus
    /// </summary>
    private Dictionary<int, Dictionary<int, Dictionary<long, OrderStatus>>> _executedOrders;
    /// <summary>
    /// ContractId.OrderId => OrderStatus
    /// </summary>
    private Dictionary<int, Dictionary<long, OrderStatus>> _triggerOnlyOrders;

    private bool _enableLogging = true;

    public VirtualExecutor(IEventIdProvider eventIdProvider, IExecIdProvider execIdProvider, ILogger<VirtualExecutor> logger)
    {
        _eventIdProvider = eventIdProvider;
        _execIdProvider = execIdProvider;
        _logger = logger;
    }

    public void Initialize(IQueryBus queryBus)
    {
        _executedOrders = queryBus.Query<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>(new(true))
            .GroupBy(o => o.ContractId)
            .ToDictionary(
                c => c.Key,
                c => c
                    .GroupBy(o => o.AccountId)
                    .ToDictionary(
                        a => a.Key,
                        a => a.ToDictionary(o => o.OrderId)
                    )
            );
        
        _triggerOnlyOrders = queryBus.Query<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>(new(false))
            .GroupBy(o => o.ContractId)
            .ToDictionary(
                gr => gr.Key,
                gr => gr.ToDictionary(o => o.OrderId)
            );
        
        if (_enableLogging)
            _logger.LogInformation($"Initialized virtual orders");
    }

    // public async Task InitializeAsync(IQueryBus queryBus)
    // {
    //     _orders = //(await accountsRepository
    //         //.GetActiveVirtualExecutorOrdersAsync())
    //         (await queryBus.QueryAsync<IReadOnlyCollection, IReadOnlyList<OrderStatus>>(new()))
    //         .GroupBy(o => o.ContractId)
    //         .ToDictionary(
    //             c => c.Key,
    //             c => c
    //                 .GroupBy(o => o.AccountId)
    //                 .ToDictionary(
    //                     a => a.Key,
    //                     a => a.ToDictionary(o => o.OrderId)
    //                 )
    //         );
    //     
    //     _logger.LogInformation($"Initialized virtual orders");
    // }
    
    public void CheckOrders(int contractId, decimal price, int? tradingSessionId, Instant referenceDt,
        Instant processingDt, IQueryBus queryBus, IEventBus eventBus, 
        StopOrdersExecution stopOrdersExecution, bool checkMarketOrdersOnly = false)
    {
        if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");

        var contract = queryBus.Query<GetContract, Contract>(new(contractId));

        if (_executedOrders.TryGetValue(contractId, out var executedOrders))
        {
            var oa = executedOrders.ToArray();
            foreach (var a in oa)
            {
                var accountId = a.Key;

                var os = a.Value.Values.ToArray();
                foreach (var o in os)
                {
                    if (checkMarketOrdersOnly && o.OrdType != OrdType.Market) continue;

                    var er = o.CheckExecution(_execIdProvider, price, contract, referenceDt, processingDt,
                        contract.PLCalculator,
                        useStopPx: stopOrdersExecution == StopOrdersExecution.StopPx,
                        tradingSessionId: tradingSessionId,
                        immediateCancellation: true);

                    if (er != null && _enableLogging)
                        _logger.LogInformation($"Order executed: {er.GetLogString()}");

                    if (er != null)
                    {
                        var account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
                        account?.ProcessExecutionReport(er, processingDt);
                        if (er.OrdStatus.IsTerminal())
                        {
                            _executedOrders[contractId][accountId].Remove(er.OrderId);
                        }
                        else
                        {
                            _executedOrders[contractId][accountId][er.OrderId] = er;
                        }
                    }
                }
            }
        }

        if (_triggerOnlyOrders.TryGetValue(contractId, out var triggeredOrders))
        {
            foreach (var order in triggeredOrders.Values)
            {
                var er = order.CheckPendingOrderActivation(_execIdProvider, price, processingDt);
                if (er != null)
                {
                    if (_enableLogging) _logger.LogInformation($"Pending order activated: {er.GetLogString()}");
                    var account = queryBus.Query<GetAccount, IAccount?>(new(order.AccountId));
                    account?.ProcessExecutionReport(er, processingDt);
                    _triggerOnlyOrders[contractId].Remove(order.OrderId);
                }
            }
        }
    }
    
    /// <summary>
    /// Used to check execution of pending orders inside a bar during backtesting.
    /// Stop Losses are executed before Take Profits. 
    /// </summary>
    /// <param name="useStopPx">Indicates if stop orders must be executed using their StopPx (recommended for high timeframes) or by the trigger price (high/low)</param>
    /// <param name="executionPrice">When useStopPx == false, it's recommended to pass the Close of the bar here, so stop orders will be executed by it and not by High/Low</param>
    public void CheckPendingOrders(int contractId, decimal high, decimal low, int? tradingSessionId,
        Instant referenceDt,
        Instant processingDt,
        StopOrdersExecution stopOrdersExecution,
        IEventBus eventBus,
        IQueryBus queryBus,
        decimal? executionPrice = null
    )
    {
        // Because this method is used only during backtesting, only orders on virtual accounts are checked
        if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
        
        if (!_executedOrders.TryGetValue(contractId, out var orders)) return;

        var contract = queryBus.Query<GetContract, Contract>(new(contractId));
        
        foreach (var a in orders.ToArray())
        {
            foreach (var o in a.Value.Values.OrderBy(o => 
                 o is { OrdType: OrdType.StopMarket, IsSltp: true } ? 0 
                 : 1
            ))
            {
                if (o.OrdType == OrdType.Market) continue;
                
                var triggerPrice =
                (
                    (o is { Side: Side.Sell, OrdType: OrdType.Limit }) // TP for long position
                    || o is { Side: Side.Buy, OrdType: OrdType.StopMarket } // SL for short position
                ) ? high : low;
                
                var er = o.CheckExecution(_execIdProvider, triggerPrice, contract, referenceDt, processingDt, contract.PLCalculator, 
                    useStopPx: stopOrdersExecution == StopOrdersExecution.StopPx,
                    tradingSessionId: tradingSessionId, immediateCancellation: true, 
                    executionPrice: stopOrdersExecution == StopOrdersExecution.BarClose ? executionPrice : null
                );
                
                if (er != null)
                {
                    var account = queryBus.Query<GetAccount, IAccount?>(new(er.AccountId));
                    account?.ProcessExecutionReport(er, processingDt);
                    if (er.OrdStatus.IsTerminal())
                    {
                        _executedOrders[contractId][er.AccountId].Remove(er.OrderId);
                    }
                }
            }
        }
    }

    public void PlaceOrder(Order order, Instant referenceDt, Instant processingDt, IQueryBus queryBus, bool isVirtualAccount)
    {
        if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
        
        var accountId = order.AccountId;
        var contractId = order.ContractId;
        
        if (!order.IsVirtual)
        {
            throw new Exception($"Trying to place a non-virtual order to virtual executor, contractId={contractId}, accountId={accountId}");
        }

        var account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
        Dictionary<long, OrderStatus> target;
        if (isVirtualAccount)
        {
            _executedOrders.TryAdd(contractId, new());
            var c = _executedOrders[contractId];
            c.TryAdd(accountId, new());
            target = c[accountId];
        }
        else
        {
            if (order.OrdType != OrdType.StopMarket && order.OrdType != OrdType.StopLimit &&
                order.OrdType != OrdType.MarketIfTouched)
            {
                var rejectEr = order.RejectOrder(_execIdProvider.GetNextExecId(), RejectReason.Other, "Unsupported order type for VE", processingDt);
                account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
                account?.ProcessExecutionReport(rejectEr, processingDt);
                return;
            }
            _triggerOnlyOrders.TryAdd(contractId, new());
            target = _triggerOnlyOrders[contractId];
        }
        
        var er = order.AcceptOrder(_execIdProvider.GetNextExecId(), referenceDt);
        target.Add(order.OrderId, er);
        account?.ProcessExecutionReport(er, processingDt);
    }
    
    public void CancelOrder(int contractId, int accountId, long orderId, Instant referenceDt, Instant processingDt,
        IQueryBus queryBus, bool isVirtualAccount)
    {
        OrderStatus? order = null;
        Dictionary<long, OrderStatus>? source;
        if (isVirtualAccount)
        {
            if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
            
            source = _executedOrders.GetValueOrDefault(contractId)?
                .GetValueOrDefault(accountId);
        }
        else
        {
            if (_triggerOnlyOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
            
            source = _triggerOnlyOrders.GetValueOrDefault(contractId);
        }
        
        order = source?.GetValueOrDefault(orderId);

        if (order == null)
        {
            // This is a state discrepancy between the account and the virtual executor.
            // Better to throw an exception in this case.
            throw new Exception("There is a state discrepancy between the account and the virtual executor: trying to cancel an order that doesn't exist");
        }
            
        source!.Remove(order.OrderId);
        var er = order.CancelOrder(_execIdProvider, processingDt, immediateCancellation: true);
        var account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
        account?.ProcessExecutionReport(er, processingDt);
    }
    
    public void ReplaceOrder(int contractId, int accountId, long orderId, OrderReplaceRequest request,
        Instant referenceDt, Instant processingDt, IQueryBus queryBus, bool isVirtualAccount)
    {
        OrderStatus? order = null;
        Dictionary<long, OrderStatus>? source;
        if (isVirtualAccount)
        {
            if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
            
            source = _executedOrders.GetValueOrDefault(contractId)?
                .GetValueOrDefault(accountId);
        }
        else
        {
            if (_triggerOnlyOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
            
            source = _triggerOnlyOrders.GetValueOrDefault(contractId);
        }
        
        order = source?.GetValueOrDefault(orderId);
        
        if (order == null)
        {
            // This is a state discrepancy between the account and the virtual executor.
            // Better to throw an exception in this case.
            throw new Exception("There is a state discrepancy between the account and the virtual executor: trying to cancel an order that doesn't exist");
        }
        
        var er = order!.ConfirmReplace(_execIdProvider.GetNextExecId(), request, processingDt);
        if (er.OrdStatus.IsTerminal())
        {
            source!.Remove(orderId);
        }
        else
        {
            source![orderId] = er;
        }
        
        var account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
        account?.ProcessExecutionReport(er, processingDt);
    }

    public IEnumerable<OrderStatus> GetVirtualAccountOrders(int accountId) => _executedOrders
        .Values
        .SelectMany(a =>
            (IEnumerable<OrderStatus>)(a.TryGetValue(accountId, out var accountOrders)
                ? accountOrders.Values
                : Array.Empty<OrderStatus>())
        );

    public void UpdatePendingOrdersOnStartup(Instant dt, IQueryBus queryBus)
    {
        if (_executedOrders == null) throw new InvalidOperationException("Virtual executor is not initialized");
        
        var pendingOrders = _executedOrders.Values
            .SelectMany(byContract => byContract.Values
                .SelectMany(byAccount => byAccount.Values
                    .Select(o => new { Order = o, IsVirtualAccount = true })
                )
            )
            .Where(o => o.Order.OrdStatus.IsPending())
            .Union(_triggerOnlyOrders.Values
                .SelectMany(byContract => byContract.Values
                    .Select(o => new {  Order = o, IsVirtualAccount = true })
                )
                .Where(o => o.Order.OrdStatus.IsPending())
            )
            .ToList();
        
        if (_enableLogging)
            _logger.LogInformation($"Updating {pendingOrders.Count} pending orders");

        foreach (var o in pendingOrders)
        {
            var order = o.Order;
            if (order.OrdStatus == OrdStatus.PendingNew)
            {
                var er = order.AcceptOrder(_execIdProvider.GetNextExecId(), dt);
                _executedOrders[er.ContractId][er.AccountId][er.OrderId] = er;
                var account = queryBus.Query<GetAccount, IAccount?>(new(er.AccountId));
                account?.ProcessExecutionReport(er, dt);
            }
            else if (order.OrdStatus == OrdStatus.PendingCancel)
            {
                CancelOrder(order.ContractId, order.AccountId, order.OrderId, dt, dt, queryBus, o.IsVirtualAccount);
            }
            else if (order.OrdStatus == OrdStatus.PendingReplace)
            {
                ReplaceOrder(order.ContractId, order.AccountId, order.OrderId, 
                    new() { OrderQty = order.OrderQty, Price = order.Price, StopPx = order.StopPx }, 
                    dt, dt, queryBus, o.IsVirtualAccount
                );
            }
        }
    }

    // public void EnsureOrderIsRemoved(int contractId, int accountId, long orderId)
    // {
    //     if (_executedOrders.TryGetValue(contractId, out var c)
    //         && c!.TryGetValue(accountId, out var a))
    //         a!.Remove(orderId);
    // }

    public void EnableLogging()
    {
        _enableLogging = true;
    }

    public void DisableLogging()
    {
        _enableLogging = false;
    }
}