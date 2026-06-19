using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/accounts")]
public class AccountsController(
    MainContext context,
    IManagementServiceClient managementClient
) : Controller
{
    [HttpGet]
    [EndpointName("GetAccounts")]
    [Produces("application/json")]
    public Task<IReadOnlyCollection<AccountListModel>> GetAccounts([FromQuery] AccountsFilter? filter) =>
        GetAccountsInternal(filter, false);

    private async Task<IReadOnlyCollection<AccountListModel>> GetAccountsInternal(AccountsFilter? filter, bool includeTradingClient)
    {
        filter ??= new();
        var query = context.Accounts
            .Where(a => 
                (filter.AccountIds == null || filter.AccountIds.Count == 0 || filter.AccountIds.Contains(a.AccountId))
                && (filter.AccountTypes == null || filter.AccountTypes.Count == 0 || filter.AccountTypes.Contains(a.AccountType))
                && (filter.StrategyId == null 
                    || (a.Strategy != null && a.Strategy.StrategyId == filter.StrategyId)
                    // || (a.EsaSubscription != null && a.EsaSubscription.StrategyId == filter.StrategyId)
                )
            )
            .Include(a => a.Currency).ThenInclude(c => c.Asset)
            .Include(a => a.Broker)
            .AsNoTracking();

        if (includeTradingClient)
        {
            query = query.Include(a => a.TradingClientConfig);
        }
        return await query.Select(a => 
            new AccountListModel(
                a, 
                a.Currency.Asset.Name,
                a.Broker != null ? a.Broker.Name : null,
                a.Broker != null ? a.Broker.BrokerType : null,
                null,
                a.Strategy!.StrategyId,
                a.Strategy.Name
            )
            {
                TradingClientConfig = includeTradingClient && a.TradingClientConfig != null
                    ? new TradingClientConfig(a.TradingClientConfig) { TradingClientSecret = null }
                    : null
            })
            .ToListAsync();
    }

    [HttpPost]
    [EndpointName("CreateAccount")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        await managementClient.CreateAccountAsync(request);
        return Ok();
    }

    [HttpGet, Route("{accountId:int}")]
    [EndpointName(nameof(GetAccount))]
    [Produces("application/json")]
    public async Task<ActionResult<AccountListModel>> GetAccount([FromRoute] int accountId)
    {
        var account = (await GetAccountsInternal(new() { AccountIds = [accountId] }, true)).SingleOrDefault();
        return account == null ? NotFound() : Ok(account);
    }

    [HttpGet, Route("{accountId:int}/subaccounts")]
    [EndpointName(nameof(GetSubaccounts))]
    [Produces("application/json")]
    public async Task<IEnumerable<SubaccountListModel>> GetSubaccounts([FromRoute] int accountId,
        [FromQuery] SubaccountsFilter? filter = null)
    {
        filter ??= new();
        return await context.Subaccounts
            .Where(sa => 
                sa.AccountId == accountId 
                && (filter.Classifier == null || sa.Classifier == filter.Classifier)
            )
            .Select(sa => new SubaccountListModel(sa, sa.Account.Name, sa.Subaccount.Name, sa.Broker!.Name))
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpPost, Route("{accountId:int}/subaccounts")]
    [EndpointName(nameof(CreateSubaccount))]
    public async Task<IActionResult> CreateSubaccount([FromRoute] int accountId, [FromBody] CreateSubaccountRequest request)
    {
        await managementClient.CreateSubaccountAsync(request);
        return Ok();
    }
    
    [HttpGet, Route("{accountId:int}/subaccount-of")]
    [EndpointName(nameof(GetSubaccountOf))]
    [Produces("application/json")]
    public async Task<IEnumerable<SubaccountListModel>> GetSubaccountOf([FromRoute] int accountId)
    {
        return await context.Subaccounts
            .Where(sa => 
                sa.SubaccountId == accountId 
            )
            .Select(sa => new SubaccountListModel(sa, sa.Account.Name, sa.Subaccount.Name, sa.Broker!.Name))
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpPost, Route("{accountId:int}/trading-client")]
    [EndpointName(nameof(CreateTradingClientConfig))]
    public async Task CreateTradingClientConfig([FromBody] CreateTradingClientConfigRequest request)
    {
        await managementClient.CreateTradingClientConfig(request.ToConfig());
    }
    
    [HttpDelete, Route("{accountId:int}/trading-client")]
    [EndpointName(nameof(DeleteTradingClientConfig))]
    public async Task DeleteTradingClientConfig([FromRoute] int accountId)
    {
        await managementClient.DeleteTradingClientConfig(accountId);
    }
        
    
    [HttpGet, Route("{accountId:int}/balances")]
    [EndpointName(nameof(GetBalances))]
	[Produces("application/json")]
	public async Task<IReadOnlyCollection<BalanceModel>> GetBalances([FromRoute] int accountId)
    {
        var balances = await managementClient.GetBalancesAsync(accountId);
        var assetIds = balances.Keys.ToList();
        var assets = (await context.Assets.Where(a => assetIds.Contains(a.AssetId)).ToListAsync())
            .ToDictionary(a => a.AssetId, a => a.Name);
        return balances.Select(kv => new BalanceModel(kv.Key, assets[kv.Key], kv.Value)).ToList();
    }

    [HttpGet, Route("balance-operations")]
    [EndpointName(nameof(GetBalanceOperationsHistory))]
    [Produces("application/json")]
    public async Task<IReadOnlyCollection<BalanceOperationHistoryModel>> GetBalanceOperationsHistory([FromQuery] BalanceOperationsFilter filter)
    {
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        
        return await context.BalanceOperations
            .Where(bo =>
                (filter.AccountId == null || bo.AccountId == filter.AccountId)
                && (filter.BalanceOperationId == null || bo.BalanceOperationId == filter.BalanceOperationId)
                && (filter.ExternalId == null || bo.ExternalId == filter.ExternalId)
                && (fromDt == null || fromDt <= bo.Dt)
                && (toDt == null || toDt >= bo.Dt)
            )
            .OrderBy(bo => bo.Dt)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .Select(bo => new BalanceOperationHistoryModel(bo.AccountServiceName, bo.BalanceOperationId, bo.AccountId,
                bo.Dt, bo.Amount, bo.AssetId, bo.Price, bo.FxRate, bo.ValueInAccountCcy, bo.ExternalId, bo.Description,
                bo.IsCorrection, bo.AffectsPnL, bo.AffectsInvestment, bo.AffectsBalance, bo.AffectsShareCount, 
                context.Accounts.Where(a => a.AccountId == bo.AccountId).Single().Name,
                context.Assets.Where(a => a.AssetId == bo.AssetId).Single().Name))
            .AsNoTracking()
            .ToListAsync();
    }
    
    [HttpPost, Route("balance-operations")]
    [EndpointName("CreateBalanceOperation")]
    public async Task<IActionResult> CreateBalanceOperation([FromRoute] int accountId, [FromBody] NewBalanceOperation request)
    {
        await managementClient.CreateBalanceOperationAsync(request);
        return Ok();
    }

    [HttpGet, Route("{accountId:int}/share-price-history")]
    [EndpointName(nameof(GetAccountSharePriceHistory))]
    [Produces("application/json")]
    public async Task<IEnumerable<SharePriceHistory>> GetAccountSharePriceHistory([FromRoute] int accountId)
    {
        return await context.SharePriceHistory
            .Where(sp => sp.AccountId == accountId)
            .OrderBy(sp => sp.Dt)
            .AsNoTracking()
            .ToListAsync();
    }
    
    [HttpGet, Route("share-price-history")]
    [EndpointName(nameof(GetSharePriceHistory))]
    [Produces("application/json")]
    public async Task<IEnumerable<SharePriceHistory>> GetSharePriceHistory([FromQuery] SharePriceHistoryFilter filter)
    {
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        
        var query = context.SharePriceHistory
            .Where(sp => sp.AccountId == filter.AccountId 
                && (filter.ChangeType == null || filter.ChangeType == sp.Type)
                && (fromDt == null || fromDt <= sp.Dt)
                && (toDt == null || toDt >= sp.Dt)
            );
            
        query = filter.SortDescending
            ? query.OrderByDescending(sp => sp.Dt)
            : query.OrderBy(sp => sp.Dt);
        
        query = query.Skip(filter.Offset).Take(filter.Limit);
            
        return await query.AsNoTracking().ToListAsync();
    }

    // [HttpGet, Route("{accountId}/metrics")]
    // [EndpointName(nameof(GetMetrics))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<FitnessTestResult>> GetMetrics([FromRoute] Guid accountId)
    // {
    //     var calculator = new FitnessResultCalculator();
    //     var spHistory = (await GetAccountSharePriceHistory(accountId)).ToList();
    //     var positionCloses = await context.PositionsHistory
    //         .Where(sp => sp.AccountId == accountId && sp.Type == PositionChangeType.Close)
    //         .OrderBy(p => p.CloseDt).ToListAsync();
    //
    //     var allResults = calculator.Calculate(accountId, accountId, spHistory, positionCloses, new List<Trade>());
    //     allResults.StringParams = "All years";
    //
    //     var res = new List<FitnessTestResult> { allResults };
    //
    //     var years = spHistory.GroupBy(sp => sp.Dt.Minus(Duration.Epsilon).InUtc().Year).ToList();
    //     if (years.Count > 1)
    //     {
    //         res.AddRange(years.OrderByDescending(gr => gr.Key).Select(gr =>
    //         {
    //             var yearStart = Instant.FromUtc(gr.Key, 1, 1, 0, 0);
    //             var yearEnd = Instant.FromUtc(gr.Key + 1, 1, 1, 0, 0);
    //             var yearResult = calculator.Calculate(accountId, accountId, gr,
    //                 positionCloses.Where(p => p.CloseDt > yearStart && p.CloseDt <= yearEnd),
    //                 new List<Trade>()
    //             );
    //             yearResult.StringParams = gr.Key.ToString();
    //             return yearResult;
    //         }));
    //     }
    //
    //     return res;
    // }

    // [HttpGet, Route("{accountId:guid}/allocations")]
    // [EndpointName(nameof(GetAllocations))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<AccountAllocationModel>> GetAllocations([FromRoute] Guid accountId)
    // {
    //     var ssa = await _queryBus.QueryAsync<GetStrategySubaccount, IStrategySubaccount>(new(accountId));
    //     var allocations = ssa.GetAllocations();
    //     var accounts = await GetAccounts(new() { AccountIds = allocations.Keys.ToList() });
    //     return accounts.Select(a => new AccountAllocationModel(a, allocations[a.AccountId])).ToList();
    // }
    
    [HttpGet, Route("{accountId:int}/positions")]
    [EndpointName("GetActivePositions")]
    [Produces("application/json")]
    public async Task<IReadOnlyCollection<PositionView>> GetActivePositions([FromRoute] int accountId)
    {
        var positions = await managementClient.GetActivePositionsAsync(accountId);
        
        if (positions.Count == 0) return Array.Empty<PositionView>();
        
        var contractIds = positions.Select(p => p.ContractId).Distinct().ToList();
        var contracts = await GetContractNames(contractIds);
        
        return positions.Select(p => new PositionView(p, "", contracts.GetValueOrDefault(p.ContractId)!)).ToList();
    }
    
    

    [HttpGet, Route("positions-history")]
    [EndpointName("GetPositionsHistory")]
    [Produces("application/json")]
    public async Task<IEnumerable<PositionView>> GetPositionsHistory([FromQuery] PositionHistoryFilter? filter = null)
    {
        filter ??= new();
        var openDtFrom = filter.OpenDtFrom.FromApiFormat();
        var openDtTo = filter.OpenDtTo.FromApiFormat();
        var historyOpenDtFrom = filter.HistoryOpenDtFrom.FromApiFormat();
        var historyOpenDtTo = filter.HistoryOpenDtTo.FromApiFormat();
        var closeDtFrom = filter.CloseDtFrom.FromApiFormat();
        var closeDtTo = filter.CloseDtTo.FromApiFormat();
        
        return await context.PositionsHistory
            .Include(p => p.Account)
            .Include(p => p.Contract)
            .Where(p =>
                (openDtFrom == null || openDtFrom <= p.OpenDt)
                && (openDtTo == null || openDtTo >= p.OpenDt)
                && (historyOpenDtFrom == null || historyOpenDtFrom <= p.HistoryOpenDt)
                && (historyOpenDtTo == null || historyOpenDtTo >= p.HistoryOpenDt)
                && (filter.AccountId == null || filter.AccountId == p.AccountId)
                && (filter.ContractId == null || filter.ContractId == p.ContractId)
                && (filter.TradeId == null || filter.TradeId == p.OpenTradeId || filter.TradeId == p.CloseTradeId)
                && (closeDtFrom == null || closeDtFrom <= p.CloseDt)
                && (closeDtTo == null || closeDtTo >= p.CloseDt)
                && (filter.Type == null || filter.Type.Contains(p.Type))
            )
            .OrderBy(p => p.CloseDt)
            .AsNoTracking()
            .Select(p => new PositionView(p, p.Account.Name, p.Contract.Ticker, p.Type))
            .ToListAsync();
    }
    
    #region Orders
    
    [HttpGet, Route("{accountId:int}/orders")]
    [EndpointName(nameof(GetActiveOrders))]
    [Produces("application/json")]
    public async Task<IEnumerable<OrderView>> GetActiveOrders([FromRoute] int accountId)
    {
        var orders = await managementClient.GetActiveOrdersAsync(accountId);
        
        if (orders.Count == 0) return Array.Empty<OrderView>();
        
        var contractIds = orders.Select(o => o.ContractId).Distinct().ToList();
        var contracts = await GetContractNames(contractIds);
        
        return orders.Select(o => 
            new OrderView(o, "", null, contracts.GetValueOrDefault(o.ContractId)!))
            .ToList();
    }

    [HttpPost, Route("orders")]
    [EndpointName(nameof(NewOrder))]
    public async Task<IActionResult> NewOrder([FromBody] NewOrderSingle nos)
    {
        await managementClient.PlaceOrderAsync(nos);
        return Ok();
    }
    
    [HttpDelete, Route("orders")]
    [EndpointName(nameof(CancelOrder))]
    public async Task<IActionResult> CancelOrder([FromBody] OrderCancelRequest ocr)
    {
        await managementClient.CancelOrderAsync(ocr);
        return Ok();
    }

    [HttpGet, Route("orders-history")]
    [EndpointName(nameof(GetOrdersHistory))]
    [Produces("application/json")]
    public async Task<IEnumerable<OrderHistoryView>> GetOrdersHistory([FromQuery] OrderFilter? filter = null)
    {
        filter ??= new();
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        
        return await context.OrdersHistory
            .Where(o =>
                (filter.AccountId == null || filter.AccountId == o.AccountId || filter.AccountId == o.BrokerAccountId)
                && (filter.OrderId == null || filter.OrderId == o.OrderId)
                && (filter.ContractId == null || filter.ContractId == o.ContractId)
                && (filter.OrdStatus == null || filter.OrdStatus == o.OrdStatus)
                && (string.IsNullOrEmpty(filter.ExternalId) || filter.ExternalId == o.ExternalId)
                && (filter.ExecutionRequestId == null || filter.ExecutionRequestId == o.ExecutionRequestId)
                && (fromDt == null || fromDt <= o.TransactTime)
                && (toDt == null || toDt >= o.TransactTime)
                && (filter.ExecType == null || filter.ExecType == o.ExecType)
            )
            .OrderBy(o => o.ExecId)
            .Select(o => new OrderHistoryView(o, o.Account.Name, o.BrokerAccount != null ? o.BrokerAccount.Name : null, o.Contract.Ticker))
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .AsNoTracking()
            .ToListAsync();
    }
    
    #endregion

    #region Trades management
    
    [HttpPost, Route("{accountId:int}/trade")]
    [EndpointName(nameof(BookTrade))]
    public async Task<IActionResult> BookTrade([FromRoute] int accountId, [FromBody] Trade trade)
    {
        throw new NotImplementedException();
    }
    
    // [HttpPost, Route("{accountId:guid}/trade/allocate")]
    // [EndpointName(nameof(AllocateTrade))]
    // public async Task<IActionResult> AllocateTrade([FromRoute] Guid accountId, [FromBody] AllocateTradeRequest request)
    // {
    //     var now = SystemClock.Instance.GetCurrentInstant();
    //     request.Dt ??= now;
    //     
    //     var account = await queryBus.QueryAsync<GetBrokerAccount, IBrokerAccount>(new(accountId));
    //     account.AllocateTrade(request.TradeId, request.AccountId, request.Dt.Value, now);
    //     
    //     await persistence.CommitAsync();
    //
    //     return Ok();
    // }
    
    [HttpGet, Route("trades-history")]
    [EndpointName("GetTradesHistory")]
    [Produces("application/json")]
    public async Task<IEnumerable<TradeView>> GetTradesHistory([FromQuery] TradeFilter? filter = null)
    {
        filter ??= new();
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        
        return await context.Trades
            .Where(t =>
                (filter.AccountId == null || filter.AccountId == t.AccountId)
                && (filter.ContractId == null || filter.ContractId == t.ContractId)
                && (filter.TradeId == null || filter.TradeId == t.TradeId)
                && (string.IsNullOrEmpty(filter.ExternalId) || filter.ExternalId == t.ExternalTradeId)
                && (fromDt == null || fromDt <= t.Dt)
                && (toDt == null || toDt >= t.Dt)
            )
            .Select(t => new TradeView(t, t.Account.Name, t.Contract.Ticker))
            .AsNoTracking()
            .ToListAsync();
    }
    
    #endregion
    
    // #region Execution requests management
    //
    // [HttpGet, Route("execution-requests")]
    // [EndpointName(nameof(GetExecutionRequests))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<ExecutionRequestListView>> GetExecutionRequests([FromQuery] ExecutionRequestsFilter? filter = null)
    // {
    //     filter ??= new();
    //
    //     return await context.ExecutionRequests
    //         .Where(er =>
    //             (filter.ExecutionRequestId == null || filter.ExecutionRequestId == er.ExecutionRequestId)
    //             && (filter.AccountId == null || er.AccountId == filter.AccountId)
    //             && (filter.ContractId == null || filter.ContractId == er.ContractId)
    //             && (filter.StrategyPositionId == null || filter.StrategyPositionId == er.StrategyPositionId)
    //             && (filter.SignalGroupId == null || filter.SignalGroupId == er.SignalGroupId)
    //             && (filter.IsActive == null || filter.IsActive == er.IsActive)
    //             && (filter.TargetPositionId == null || filter.TargetPositionId == er.TargetPositionId)
    //         )
    //         .Include(er => er.Status)
    //         .AsNoTracking()
    //         .Select(er => new ExecutionRequestListView(er.Status, er.Status))
    //         .ToListAsync();
    // }
    //
    // [HttpPost, Route("{accountId:guid}/execution-requests")]
    // [EndpointName(nameof(CreateExecutionRequest))]
    // public async Task<IActionResult> CreateExecutionRequest([FromRoute] Guid accountId, [FromBody] CreateExecutionRequestRequest request)
    // {
    //     await commandBus.SendCommandAsync(new CreateExecutionRequestCmd(accountId, request.TargetPositionId, request.SignedQty,
    //         request.Price, request.PositionEffect, SystemClock.Instance.GetCurrentInstant(), 
    //         request.NumberOfRetries, request.ExecutionPolicyId
    //     ));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // [HttpPost, Route("{accountId:guid}/execution-requests/{execRId:guid}")]
    // [EndpointName(nameof(RetryExecutionRequest))]
    // public async Task<IActionResult> RetryExecutionRequest([FromRoute] Guid accountId, [FromRoute] Guid execRId)
    // {
    //     await commandBus.SendCommandAsync(new RetryExecutionRequestCmd(accountId, execRId));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // [HttpDelete, Route("{accountId:guid}/execution-requests/{execRId:guid}")]
    // [EndpointName(nameof(CancelExecutionRequest))]
    // public async Task<IActionResult> CancelExecutionRequest([FromRoute] Guid accountId, [FromRoute] Guid execRId, [FromQuery] string? cxlReason = null)
    // {
    //     cxlReason ??= "Manual cancellation";
    //     
    //     await commandBus.SendCommandAsync(new CancelExecutionRequestCmd(accountId, execRId, cxlReason));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // #endregion
    

    // [HttpPost, Route("{accountId:guid}/fund/subscription")]
    // [EndpointName(nameof(CreateFundSubscription))]
    // public async Task<IActionResult> CreateFundSubscription([FromRoute] Guid accountId, [FromBody] FundSubscriptionRequest request)
    // {
    //     await commandBus.SendCommandAsync(new SubscribeFundToBookCmd(accountId, request.BookId, request.ReplicationFactor));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // [HttpPost, Route("{accountId:guid}/esa/subscription")]
    // [EndpointName(nameof(CreateEsaSubscription))]
    // public async Task<IActionResult> CreateEsaSubscription([FromRoute] Guid accountId, [FromBody] EsaSubscriptionRequest request)
    // {
    //     await commandBus.SendCommandAsync(new SubscribeEsaToStrategyCmd(accountId, request));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // [HttpGet, Route("execution-policies")]
    // [EndpointName(nameof(GetExecutionPolicies))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<ExecutionPolicyDefinition>> GetExecutionPolicies([FromQuery] ExecutionPoliciesFilter filter) =>
    //     await executionPoliciesRepository.GetExecutionPoliciesAsync();
    //
    // [HttpGet, Route("{accountId:guid}/broker-account-assignments")]
    // [EndpointName(nameof(GetBrokerAccountsForSsa))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<BrokerAccountSubscription>> GetBrokerAccountsForSsa([FromRoute] Guid accountId)
    // {
    //     return await context.Accounts
    //         .Include(a => a.Subaccounts.Where(sa => sa.Classifier == IStrategySubaccountState.BrokerAccountClassifier))
    //         .ThenInclude(sa => sa.Subaccount)
    //         .ThenInclude(a => a.Broker)
    //         .Where(a => a.AccountId == accountId)
    //         .AsNoTracking()
    //         .SelectMany(a => a.Subaccounts.Select(sa => new BrokerAccountSubscription(
    //             new(sa.SubaccountId, sa.Subaccount.AccountType, sa.Subaccount.Name),
    //             sa.Subaccount.BrokerId!.Value,
    //             sa.Subaccount.Broker!.Name
    //         )))
    //         .ToListAsync();
    // }
    //
    // [HttpPost, Route("{accountId:guid}/broker-account-assignments")]
    // [EndpointName(nameof(AssignBrokerAccountToSsa))]
    // public async Task<IActionResult> AssignBrokerAccountToSsa([FromRoute] Guid accountId, [FromBody] AssignSsaToBrokerAccountRequest request)
    // {
    //     await commandBus.SendCommandAsync(new AssignSsaToBrokerAccountCmd(accountId, request.BrokerAccountId));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }

    // #region Target positions
    //
    // [HttpGet, Route("{accountId:guid}/target-positions")]
    // [EndpointName(nameof(GetTargetPositions))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<TargetPositionListView>> GetTargetPositions([FromRoute] Guid accountId)
    // {
    //     var account = await queryBus.QueryAsync<GetExecutableSubaccount, IExecutableSubaccount>(new(accountId));
    //     return account.GetReconciledPositions();
    //     // var newInvestment = account.AccountState.GetInvestmentInfo().Investment;
    //     // var strategyInvestment = 100000; // TODO need to get the correct strategy investment from the signal group and and the account
    //     //
    //     // return (await _context.TargetPositionsView
    //     //     .Where(tp => tp.AccountId == accountId)
    //     //     .AsNoTracking()
    //     //     .ToListAsync()
    //     //     ).Select(tp =>
    //     //     {
    //     //         var contract = _queryBus.Query<GetContract, Contract>(new(tp.ContractId));
    //     //         var expectedTV = contract.NormalizeVolume(strategyInvestment == 0 ? 0 : tp.Investment / strategyInvestment * tp.StrategyVolume);
    //     //         var expectedNewInvTv = newInvestment != tp.Investment 
    //     //             ? contract.NormalizeVolume(strategyInvestment == 0 ? 0 : newInvestment / strategyInvestment * tp.StrategyVolume)
    //     //             : (decimal?)null;
    //     //         
    //     //         return new TargetPositionListView(tp.AccountId, tp.TargetPositionId, tp.PositionId, tp.SignalGroupId,
    //     //             tp.ContractId, tp.Ticker, tp.StrategyPositionId, tp.StrategyVolume, tp.StrategyOpenPrice,
    //     //             tp.TargetVolume, tp.ActualVolume, tp.ActualOpenPrice, tp.Pending, tp.InProgress, tp.ActiveExecRsCount, tp.ExecRsCount, 
    //     //             expectedTV, expectedNewInvTv);
    //     //     });
    // }
    //
    // [HttpGet, Route("target-positions-history")]
    // [EndpointName(nameof(GetTargetPositionsHistory))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<TargetPositionHistoryListView>> GetTargetPositionsHistory([FromQuery] TargetPositionsHistoryFilter? filter = null)
    // {
    //     filter ??= new();
    //
    //     return (await context.TargetPositionsHistory
    //             .Where(tp =>
    //                 (filter.AccountId == null || tp.AccountId == filter.AccountId)
    //                 && (filter.ContractId == null || tp.ContractId == filter.ContractId)
    //                 && (filter.StrategyPositionId == null || tp.StrategyPositionId == filter.StrategyPositionId)
    //                 && (filter.SignalGroupId == null || tp.SignalGroupId == filter.SignalGroupId)
    //                 && (fromDt == null || tp.Dt >= fromDt)
    //                 && (toDt == null || tp.Dt < toDt)
    //             )
    //             .Include(tp => tp.Account)
    //             .Include(tp => tp.Contract)
    //             .Include(tp => tp.ExecutionRequests).ThenInclude(er => er.Status)
    //             .Include(tp => tp.Trade)
    //             .OrderByDescending(tp => tp.Dt)
    //             .Skip(filter.Offset)
    //             .Take(filter.Limit)
    //             .ToListAsync())
    //         .Select(tp =>
    //         {
    //             var ers = tp.ExecutionRequests.Count > 0 ?
    //                 tp.ExecutionRequests
    //                     .Select(er => new
    //                     {
    //                         Count = 1, ActiveCount = er.IsActive ? 1 : 0, Filled = er.Status.SignedFilledQty,
    //                         Pending = er.Status.SignedPendingQty,
    //                         InProgress = er.IsActive
    //                             ? er.Status.SignedQty - er.Status.SignedFilledQty - er.Status.SignedPendingQty
    //                             : 0
    //                     })
    //                     .Aggregate((memo, i) => new
    //                     {
    //                         Count = memo.Count + i.Count,
    //                         ActiveCount = memo.ActiveCount + i.ActiveCount,
    //                         Filled = memo.Filled + i.Filled,
    //                         Pending = memo.Pending + i.Pending,
    //                         InProgress = memo.InProgress + i.InProgress,
    //                     })
    //                 : new
    //                 {
    //                     Count = 0,
    //                     ActiveCount = 0,
    //                     Filled = 0m,
    //                     Pending = 0m,
    //                     InProgress = 0m,
    //                 };
    //
    //             return new TargetPositionHistoryListView(tp, tp.Account.Name, tp.Contract.Ticker, tp.Trade?.Price,
    //                 ers.Count, ers.ActiveCount, ers.Filled, ers.Pending, ers.InProgress, null); // TODO
    //         });
    // }
    //
    // [HttpPost, Route("{accountId:guid}/target-positions/reconcile")]
    // [EndpointName(nameof(ReconcileTargetPositions))]
    // public async Task<IActionResult> ReconcileTargetPositions([FromRoute] Guid accountId)
    // {
    //     await commandBus.SendCommandAsync(new ReconcileTargetPositionsCmd(accountId));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // #endregion
    
    // #region Fund account
    //
    // [HttpGet, Route("{accountId:guid}/fund/trades")]
    // [EndpointName(nameof(GetFundAccountTrades))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<FundAccountTradeView>> GetFundAccountTrades([FromRoute] Guid accountId, [FromQuery] FundAccountTradesFilter? filter = null)
    // {
    //     filter ??= new();
    //     
    //     var account = (await GetAccounts(new() { AccountId = accountId })).SingleOrDefault();
    //     
    //     // TODO: this cannot show the results if the subscription has been removed recently
    //     if (account?.AccountType != AccountType.Fund || account?.BookSubscription == null)
    //         return new List<FundAccountTradeView>();
    //
    //     var bookId = account.BookSubscription.BookId;
    //     
    //     // TODO: use GetEffectiveStrategyFactors
    //     var book = await queryBus.QueryAsync<GetBook, IBook>(new(bookId));
    //     var strategyIds = book.GetEffectiveStrategyFactors().Keys.ToList();
    //     
    //     var trades = await context.Trades
    //         .Where(t => 
    //             t.Account.Strategy != null 
    //             && strategyIds.Contains(t.Account.Strategy.StrategyId)
    //             && (!filter.From.HasValue || t.Dt >= filter.From) 
    //             && (!filter.To.HasValue || t.Dt < filter.To)
    //         )
    //         .Include(t => t.Account)
    //             .ThenInclude(a => a.Strategy)
    //         .Include(t => t.Contract)
    //         .AsNoTracking()
    //         .Select(t => new FundAccountTradeView(t, t.Account.Name, t.Contract.Ticker, t.Account.Strategy!.Name, t.Account.Strategy.StrategyId))
    //         .ToListAsync();
    //
    //     return trades;
    // }
    //
    // [HttpGet, Route("{accountId:guid}/fund/strategy-subaccounts")]
    // [EndpointName(nameof(GetStrategySubaccounts))]
    // [Produces("application/json")]
    // public async Task<IReadOnlyCollection<StrategySubaccountListView>> GetStrategySubaccounts([FromRoute] Guid accountId)
    // {
    //     var account = (await GetAccounts(new() { AccountId = accountId })).SingleOrDefault();
    //     
    //     // TODO: this cannot show the results if the subscription has been removed recently
    //     if (account?.AccountType != AccountType.Fund || account?.BookSubscription == null)
    //         return new List<StrategySubaccountListView>();
    //
    //     var bookId = account.BookSubscription.BookId;
    //     
    //     // TODO: use GetEffectiveStrategyFactors
    //     var book = await queryBus.QueryAsync<GetBook, IBook>(new(bookId));
    //     var factors = book.GetEffectiveStrategyFactors();
    //     var strategyIds = factors.Keys.ToList();
    //
    //     var strategies = await context.Strategies
    //         .Where(s => strategyIds.Contains(s.StrategyId))
    //         .Include(s => s.Account)
    //         .ThenInclude(a => a.Currency).ThenInclude(c => c.Asset)
    //         .AsNoTracking()
    //         .ToListAsync();
    //     
    //     var subaccounts = await context.Subaccounts
    //         .Where(sa => 
    //             sa.AccountId == accountId 
    //             && sa.Classifier ==  IFundAccountState.StrategySubaccountClassifier
    //         )
    //         .Include(sa => sa.Subaccount)
    //             .ThenInclude(a => a.Currency)
    //                 .ThenInclude(c => c.Asset)
    //         .Include(sa => sa.Subaccount)
    //             .ThenInclude(sa => sa.InvestmentHistory.OrderByDescending(ih => ih.Dt).Take(1))
    //         // .Include(sa => sa.Subaccount)
    //         //     .ThenInclude(a => a.Subaccounts
    //         //         .Where(sa => sa.Classifier == IStrategySubaccountState.FundAccountClassifier && sa.CorrelationAccountId == accountId).Take(1)
    //         //     )
    //         //         .ThenInclude(sa => sa.Subaccount)
    //         //             .ThenInclude(a => a.InvestmentHistory.OrderByDescending(ih => ih.Dt).Take(1))
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     var res = strategies.FullOuterJoin(subaccounts,
    //         s => s.StrategyId,
    //         sa => sa.CorrelationAccountId,
    //         (s, sa, id) => new StrategySubaccountListView
    //         {
    //             StrategyId = id,
    //             StrategyName = s?.Name,
    //             StrategyCcyId = s?.Account?.CurrencyId,
    //             StrategyCcyName = s?.Account?.Currency?.Asset?.Name,
    //             AccountId = sa?.SubaccountId,
    //             AccountName = sa?.Subaccount?.Name,
    //             AccountType = sa?.Subaccount?.AccountType,
    //             AccountCcyId = sa?.Subaccount?.CurrencyId,
    //             AccountCcyName = sa?.Subaccount?.Currency?.Asset?.Name,
    //             DesiredInvestment = 0,
    //             Share = factors.GetValueOrDefault(id!.Value, 0),
    //             // SubaccountId = sa?.Subaccount?.Subaccounts?.SingleOrDefault()?.SubaccountId,
    //             // Investment = sa?.Subaccount?.Subaccounts?.SingleOrDefault()?.Subaccount.InvestmentHistory.SingleOrDefault()?.Investment ?? 0,
    //             Investment = sa?.Subaccount?.InvestmentHistory.SingleOrDefault()?.Investment ?? 0,
    //         });
    //
    //     return res.ToList();
    // }
    //
    // [HttpPost, Route("{accountId:guid}/fund/strategy-subaccounts")]
    // [EndpointName(nameof(AssignStrategySubaccount))]
    // public async Task<IActionResult> AssignStrategySubaccount([FromRoute] Guid accountId, [FromBody] AssignSsaToFundAccountRequest request)
    // {
    //     await commandBus.SendCommandAsync(new AssignSsaToFundCmd(accountId, request.StrategyId, request.SsaId));
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    // #endregion
    
    private async Task<Dictionary<int, string>> GetContractNames(List<int> contractIds) =>
        (await context.Contracts
            .Where(c => contractIds.Contains(c.ContractId))
            .Select(c => new { c.ContractId, c.Ticker })
            .AsNoTracking()
            .ToListAsync()
        ).ToDictionary(c => c.ContractId, c => c.Ticker);
}