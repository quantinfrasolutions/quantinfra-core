using Microsoft.AspNetCore.Http;
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
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;
using ValidationProblemDetails = Microsoft.AspNetCore.Mvc.ValidationProblemDetails;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/accounts")]
public class AccountsController(
    MainContext context,
    IManagementServiceClient managementClient
) : Controller
{
    [HttpGet]
    [EndpointName(nameof(GetAccounts))]
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
    [EndpointName(nameof(CreateAccount))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.Name)) ModelState.AddModelError(nameof(request.Name), "Name is required");
        else
        {
            var existingAcc = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Name.ToLower() == request.Name!.ToLower());
            if (existingAcc != null) ModelState.AddModelError(nameof(request.Name), $"Duplicate name ({existingAcc.AccountId})");
        }
        
        if (request.CurrencyId == 0) ModelState.AddModelError(nameof(request.CurrencyId), $"Currency is required");
        else
        {
            var currency = await context.Currencies.AsNoTracking().SingleOrDefaultAsync(c => c.CurrencyId == request.CurrencyId);
            if (currency is null) ModelState.AddModelError(nameof(request.CurrencyId), $"Currency {request.CurrencyId} not found");
        }
        
        if (request.AccountType != AccountType.BrokerAccount) ModelState.AddModelError(nameof(request.AccountType), $"Creating accounts of type {request.AccountType} is not allowed");

        if (request.AccountType == AccountType.BrokerAccount)
        {
            if (!request.BrokerId.HasValue || request.BrokerId == 0) ModelState.AddModelError(nameof(request.BrokerId), $"Broker is required");
            else
            {
                if (await context.Brokers.AsNoTracking().SingleOrDefaultAsync(b => b.BrokerId == request.BrokerId!.Value) is null)
                    ModelState.AddModelError(nameof(request.BrokerId), $"Broker {request.BrokerId} not found");
            }
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var accountId = await managementClient.CreateAccountAsync(request);
        return CreatedAtAction(
            nameof(GetAccounts),
            null,
            accountId
        );
    }

    [HttpGet, Route("{accountId:int}")]
    [EndpointName(nameof(GetAccount))]
    [Produces("application/json")]
    public async Task<ActionResult<AccountListModel>> GetAccount([FromRoute] int accountId)
    {
        var account = (await GetAccountsInternal(new() { AccountIds = [accountId] }, true)).SingleOrDefault();
        return account == null ? NotFound() : Ok(account);
    }
    
    [HttpGet, Route("{accountId:int}/broker-reconciliation-status")]
    [EndpointName(nameof(GetBrokerAccountReconciliationStatus))]
    [Produces("application/json")]
    public async Task<ActionResult<BrokerAccountReconciliationStatus?>> GetBrokerAccountReconciliationStatus([FromRoute] int accountId)
    {
        var account = (await GetAccountsInternal(new() { AccountIds = [accountId] }, true)).SingleOrDefault();
        if (account is null) return NotFound();
        if (account.AccountType != AccountType.BrokerAccount) return BadRequest($"Account {accountId} is not broker account");
        return await managementClient.GetBrokerAccountReconciliationStatusAsync(accountId);
    }
    
    [HttpPost, Route("{accountId:int}/broker-reconciliation-status")]
    [EndpointName(nameof(Reconcile))]
    public async Task<IActionResult> Reconcile([FromRoute] int accountId)
    {
        var account = (await GetAccountsInternal(new() { AccountIds = [accountId] }, true)).SingleOrDefault();
        if (account is null) return NotFound();
        if (account.AccountType != AccountType.BrokerAccount) return BadRequest($"Account {accountId} is not broker account");
        await managementClient.Reconcile(accountId);
        return Ok();
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubaccount([FromRoute] int accountId, [FromBody] CreateSubaccountRequest request)
    {
        if (request.AccountId == 0) ModelState.AddModelError(nameof(request.AccountId), $"Account is required");
        else
        {
            var account = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == request.AccountId);
            if (account is null) ModelState.AddModelError(nameof(request.AccountId), $"Account {accountId} not found");
        }
        
        if (request.SubaccountId == 0) ModelState.AddModelError(nameof(request.SubaccountId), $"Account is required");
        else
        {
            var account = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == request.SubaccountId);
            if (account is null) ModelState.AddModelError(nameof(request.SubaccountId), $"Account {request.SubaccountId} not found");
            else
            {
                if (request.Classifier == SubaccountType.Broker)
                {
                    if (account.AccountType != AccountType.BrokerAccount) ModelState.AddModelError(nameof(request.SubaccountId), $"Account {request.SubaccountId} is not broker account");
                }
            }
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointName(nameof(CreateTradingClientConfig))]
    public async Task<IActionResult> CreateTradingClientConfig([FromBody] CreateTradingClientConfigRequest request)
    {
        if (request.AccountId == 0) ModelState.AddModelError(nameof(request.AccountId), $"Account is required");
        else
        {
            var account = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == request.AccountId);
            if (account is null) ModelState.AddModelError(nameof(request.AccountId), $"Account {request.AccountId} not found");
        }
        
        if (string.IsNullOrEmpty(request.ExecutionServiceName)) ModelState.AddModelError(nameof(request.ExecutionServiceName), $"Execution service is required");
        else
        {
            var es = await context.ExecutionServiceInstances.AsNoTracking().SingleOrDefaultAsync(s => s.Name == request.ExecutionServiceName);
            if (es is null) ModelState.AddModelError(nameof(request.ExecutionServiceName), $"Execution service {request.ExecutionServiceName} not found");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        await managementClient.CreateTradingClientConfig(request.ToConfig());
        return Ok();
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
    [EndpointName(nameof(CreateBalanceOperation))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBalanceOperation([FromRoute] int accountId, [FromBody] NewBalanceOperation request)
    {
        if (request.AccountId == 0) ModelState.AddModelError(nameof(request.AccountId), "Account is required");
        else
        {
            var acc =  await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == request.AccountId);
            if (acc == null) ModelState.AddModelError(nameof(request.AccountId), $"Account {request.AccountId} not found");
        }
        
        if (request.AssetId == 0) ModelState.AddModelError(nameof(request.AssetId), "Asset is required");
        else
        {
            var asset = await context.Assets.AsNoTracking().SingleOrDefaultAsync(a => a.AssetId == request.AssetId);
            if (asset is null) ModelState.AddModelError(nameof(request.AssetId), $"Asset {request.AssetId} not found");
            else if (asset.AssetType != AssetType.Currency) ModelState.AddModelError(nameof(request.AssetId), $"Asset {request.AssetId} is not a currency");
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        if (!request.IsCorrection)
        {
            request.AffectsPnL = false;
            request.AffectsBalance = true;
            request.AffectsInvestment = true;
            request.AffectsShareCount = true;
        }
        
        var boId = await managementClient.CreateBalanceOperationAsync(request);
        return CreatedAtAction(
            nameof(GetBalanceOperationsHistory),
            null,
            boId
        );
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
        var positions = await managementClient.GetActivePositionsAsync(accountId, true);
        
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> NewOrder([FromBody] NewOrderSingle nos)
    {
        if (nos.AccountId == 0) ModelState.AddModelError(nameof(nos.AccountId), "Account is required");
        else
        {
            var account = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.AccountId == nos.AccountId);
            if (account is null) ModelState.AddModelError(nameof(nos.AccountId), $"Account {nos.AccountId} not found");
        }

        Contract? contract = null;
        if (nos.ContractId == 0) ModelState.AddModelError(nameof(nos.ContractId), "Contract is required");
        else
        {
            contract = await context.Contracts
                .Include(c => c.Template)
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.ContractId == nos.ContractId);
            if (contract is null) ModelState.AddModelError(nameof(nos.ContractId), $"Contract {nos.ContractId} not found");
        }

        if (nos.OrdType == OrdType.Limit || nos.OrdType == OrdType.StopLimit || nos.OrdType == OrdType.MarketIfTouched)
        {
            if (!nos.Price.HasValue) ModelState.AddModelError(nameof(nos.Price), "Price is required");
            if (nos.Price.HasValue && contract is not null)
            {
                if (contract.NormalizePrice(nos.Price.Value) != nos.Price.Value)
                    ModelState.AddModelError(nameof(nos.Price), $"Tick is {contract.Template.TickSize}");
            }
        }

        if (nos.OrdType == OrdType.StopLimit || nos.OrdType == OrdType.StopMarket)
        {
            if (!nos.StopPx.HasValue) ModelState.AddModelError(nameof(nos.StopPx), "StopPx is required");
            if (nos.StopPx.HasValue && contract is not null)
            {
                if (contract.NormalizePrice(nos.StopPx.Value) != nos.StopPx.Value)
                    ModelState.AddModelError(nameof(nos.StopPx), $"Tick is {contract.Template.TickSize}");
            }
        }
        
        if (nos.OrderQty <= 0) ModelState.AddModelError(nameof(nos.OrderQty), "OrderQty must be positive");
        if (contract is not null)
        {
            if (nos.OrderQty < contract.Template.MinSize)
                ModelState.AddModelError(nameof(nos.OrderQty), $"Min size is {contract.Template.MinSize}");
            if (nos.OrderQty > contract.Template.MaxSize)
                ModelState.AddModelError(nameof(nos.OrderQty), $"Max size is {contract.Template.MaxSize}");
            if (contract.NormalizeVolume(nos.OrderQty) != nos.OrderQty)
                ModelState.AddModelError(nameof(nos.OrderQty), $"Size increment is {contract.Template.SizeIncrement}");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
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
    
    
    private async Task<Dictionary<int, string>> GetContractNames(List<int> contractIds) =>
        (await context.Contracts
            .Where(c => contractIds.Contains(c.ContractId))
            .Select(c => new { c.ContractId, c.Ticker })
            .AsNoTracking()
            .ToListAsync()
        ).ToDictionary(c => c.ContractId, c => c.Ticker);
}