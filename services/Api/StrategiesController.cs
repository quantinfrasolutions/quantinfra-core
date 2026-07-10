using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QuantInfra.Common.Interfaces.Api;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Common.Interfaces.Api.MarketData;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;
using ValidationProblemDetails = Microsoft.AspNetCore.Mvc.ValidationProblemDetails;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/strategies")]
public class StrategiesController(
    MainContext context,
    IManagementServiceClient managementClient
) : Controller
{
    [HttpGet]
    [EndpointName("GetStrategies")]
    [Produces("application/json")]
    public async Task<IEnumerable<StrategyViewBrief>> GetStrategies([FromQuery] StrategiesFilter? filter)
    {
        filter ??= new();
        
        var strategies = await context.Strategies
            .AsNoTracking()
            .Include(s => s.Account)
                .ThenInclude(a => a.Currency)
                    .ThenInclude(c => c.Asset)
            .Include(s => s.Account)
                .ThenInclude(a => a.Broker)
            .Where(s => (filter.Status == null || filter.Status.Contains(s.Status))
                && (filter.ClassNames == null || filter.ClassNames.Count == 0 || filter.ClassNames.Contains(s.ClassName))
                && (filter.StrategyIds == null || filter.StrategyIds.Count == 0 || filter.StrategyIds.Contains(s.StrategyId))
            )
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToListAsync();

        var contractIds = strategies.SelectMany(s => s.RequiredBarStorages
            .Where(bs => bs.Value.IdType == IdType.Contract)
            .Select(bs => bs.Value.Id)
        ).Union(strategies.SelectMany(s => s.Symbols.Values)).Distinct().ToList();

        var streamIds = strategies.SelectMany(s => s.RequiredBarStorages
            .Where(bs => bs.Value.IdType == IdType.Stream)
            .Select(bs => bs.Value.Id)
        ).Distinct().ToList();

        var contracts = (await context.Contracts.Where(c => contractIds.Contains(c.ContractId)).ToListAsync())
            .ToDictionary(c => c.ContractId, c => c.Ticker);

        var streams = (await context.Streams.Where(s => streamIds.Contains(s.StreamId)).ToListAsync())
            .ToDictionary(s => s.StreamId, s => s.Ticker);
        
        // var options = new JsonSerializerOptions() { WriteIndented = true };
        // options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        // options.Converters.Add(new JsonStringEnumConverter());
        
        
        return strategies.Select(s => new StrategyViewBrief(s.StrategyId, s.Name, s.ClassName, s.Params,
            s.Symbols.ToDictionary(kv => kv.Key, kv => new BriefView<int>(kv.Value, contracts[kv.Value])),
            s.RequiredBarStorages.ToDictionary(
                bs => bs.Key,
                bs => new BarStorageView(bs.Value, bs.Value.IdType == IdType.Contract ? contracts[bs.Value.Id] : streams[bs.Value.Id])
            ),
            s.Status, s.UseSignalGroups,
            new(s.Account, s.Account.Currency.Asset.Name, s.Account.Broker?.Name, s.Account.Broker?.BrokerType,
                null, s.StrategyId, s.Name),
            new BriefView<int>(s.Account.CurrencyId, s.Account.Currency.Asset.Name),
            s.StrategyServiceName, s.LiquidationParameters
        ));
    }

    [HttpPost]
    [EndpointName("CreateStrategy")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        await managementClient.CreateStrategyAsync(request);
        return Ok();
    }
    
    [HttpPost, Route("validate")]
    [EndpointName(nameof(ValidateStrategyRequest))]
    [ProducesResponseType(typeof(ValidateStrategyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ValidateStrategyResult> ValidateStrategyRequest([FromBody] CreateStrategyRequest request)
    {
        try
        {
            var contractIds = request
                .RequiredBarStorages
                .Where(bs => bs.Value.IdType == IdType.Contract)
                .Select(bs => bs.Value.Id)
                .Union(request.Symbols.Values)
                .Distinct()
                .ToList();
            
            var streamIds = request
                .RequiredBarStorages
                .Where(bs => bs.Value.IdType == IdType.Stream)
                .Select(bs => bs.Value.Id)
                .Distinct()
                .ToList();
            
            var contracts = (await context.Contracts.Where(c => contractIds.Contains(c.ContractId)).AsNoTracking().ToListAsync())
                .ToDictionary(c => c.ContractId, c => c.Ticker);
            
            var missingContracts = contractIds.Except(contracts.Keys).ToList();
            
            var streams = (await context.Streams.Where(s => streamIds.Contains(s.StreamId)).AsNoTracking().ToListAsync())
                .ToDictionary(s => s.StreamId, s => s.Ticker);
            
            var missingStreams = streamIds.Except(streams.Keys).ToList();

            var currency = await context.Currencies
                .Where(c => c.CurrencyId == request.Account.CurrencyId)
                .Include(c => c.Asset)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var accS = await context.AccountServiceInstances.SingleOrDefaultAsync(a =>
                a.Name == request.Account.AccountServiceName);
            var ss = await context.StrategyServiceInstances.SingleOrDefaultAsync(s =>
                s.Name == request.StrategyServiceName);            
            
            if (missingContracts.Any() || missingStreams.Any() || currency == null || accS == null || ss == null)
            {
                var err = new ValidateStrategyResult()
                {
                    Success = false,
                    Errors = missingContracts.Select(c => $"Contract not found by id {c}")
                        .Union(missingStreams.Select(s => $"Stream not found by id {s}"))
                        .ToList()
                };
                if (currency == null)
                {
                    err.Errors.Add($"Currency not found by id {request.Account.CurrencyId}");
                }

                if (accS == null)
                {
                    err.Errors.Add($"Account service {request.Account.AccountServiceName} doesn't exist");
                }

                if (ss == null)
                {
                    err.Errors.Add($"Strategies service {request.StrategyServiceName} doesn't exist");
                }

                return err;
            }
                
            
            var res = new StrategyViewBrief(0, request.Name, request.ClassName, request.Params,
                request.Symbols.ToDictionary(kv => kv.Key, kv => new BriefView<int>(kv.Value, contracts[kv.Value])),
                request.RequiredBarStorages.ToDictionary(
                    kv => kv.Key, 
                    kv => new BarStorageView(kv.Value, 
                        kv.Value.IdType == IdType.Contract ?  contracts[kv.Value.Id] : streams[kv.Value.Id])
                    ),
                request.StartImmediately ? StrategyStatus.Running : StrategyStatus.Stopped,
                request.UseSignalGroups,
                new AccountListModel(
                    new(request.Account.AccountServiceName, request.Account.Name, request.Account.CurrencyId, 
                        request.Account.AccountType, request.Account.PositionAccounting, null, 
                        request.Account.EnableSharePriceTracking, request.Account.IncludeUnrealizedPnLToMtm, null),
                    currency.Asset.Name, null, null,
                    null, 0, request.Name
                ),
                new(currency.CurrencyId, currency.Asset.Name),
                request.StrategyServiceName,
                null //request.LiquidationParameters
            );

            return new ValidateStrategyResult
            {
                Success = true,
                Strategy = res,
            };
        }
        catch (Exception e)
        {
            return new ValidateStrategyResult
            {
                Success = false,
                Errors = [e.Message],
            };
        }
    }
    
    [HttpGet, Route("classes")]
    [EndpointName(nameof(GetStrategyClasses))]
    [Produces("application/json")]
    public IEnumerable<StrategyTypeDescription> GetStrategyClasses() =>
        throw new NotImplementedException();
        // hostedStrategiesFactory.SupportedStrategyClasses.ToList();
    
    [Route("classes/{className}")]
    [HttpGet]
    [EndpointName(nameof(GetStrategyClass))]
    [Produces("application/json")]
    public StrategyTypeDescription GetStrategyClass([FromRoute] string className) =>
        throw new NotImplementedException();
        // hostedStrategiesFactory.SupportedStrategyClasses.Single(c => c.FullName == className);

    [Route("{strategyId:int}")]
    [HttpGet]
    [EndpointName(nameof(GetStrategy))]
    [Produces("application/json")]
    public async Task<ActionResult<StrategyViewBrief>> GetStrategy([FromRoute] int strategyId)
    {
        var strategy = (await GetStrategies(new() { StrategyIds = [strategyId] })).SingleOrDefault();
        return strategy == null ? NotFound() : Ok(strategy);
    }

    [Route("{strategyId:int}/start")]
    [HttpPost]
    [EndpointName("StartStrategy")]
    public async Task<IActionResult> StartStrategy([FromRoute] int strategyId)
    {
        await managementClient.StartStrategyAsync(strategyId);
        return Ok();
    }
    
    [Route("{strategyId:int}/stop")]
    [HttpPost]
    [EndpointName("StopStrategy")]
    public async Task<IActionResult> StopStrategy([FromRoute] int strategyId, [FromBody] StopStrategyRequest request)
    {
        throw new NotImplementedException();
        return Ok();
    }

    // /// <summary>
    // /// Returns list of accounts on which the strategy executes (includes ESAs and zero or one SSA)
    // /// </summary>
    // [HttpGet, Route("{strategyId:guid}/esas")]
    // [EndpointName(nameof(GetExecutionAccountsForStrategy))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<AccountViewBriefWithInvestment>> GetExecutionAccountsForStrategy([FromRoute] Guid strategyId)
    // {
    //     var strategy = (await GetStrategies(new() { StrategyIds = [strategyId] })).Single();
    //     
    //     var executionAccountsIds = accountsRepository.GetExecutableSubaccountsByStrategyId(strategyId).ToList();
    //     if (strategy.Account.AccountType == AccountType.StrategySubAccount) executionAccountsIds.Add(strategy.Account.AccountId);
    //
    //     var res = executionAccountsIds.Select(id =>
    //     {
    //         var account = queryBus.Query<GetAccount, IAccount>(new(id));
    //         return new AccountViewBriefWithInvestment(account.AccountId, account.AccountType,
    //             account.AccountRecord.Name,
    //             account.GetInvestment());
    //     }).ToList();
    //
    //     return res;
    // }
    //
    // [HttpPost, Route("{strategyId:guid}/position")]
    // [EndpointName(nameof(OpenPosition))]
    // public async Task<IActionResult> OpenPosition([FromRoute] Guid strategyId, [FromBody] OpenClosePositionRequest request)
    // {
    //     var strategy = await queryBus.QueryAsync<GetStrategy, IStrategyBase>(new(strategyId));
    //     strategy.OpenPosition(request.Symbol, request.Factor * request.Side.GetSign(), clock.GetCurrentInstant(), request.StrategyPositionId,
    //         request.Price, request.ClOrdId);
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // [HttpDelete, Route("{strategyId:guid}/position")]
    // [EndpointName(nameof(ClosePosition))]
    // public async Task<IActionResult> ClosePosition([FromRoute] Guid strategyId, [FromBody] OpenClosePositionRequest request)
    // {
    //     var strategy = await queryBus.QueryAsync<GetStrategy, IStrategyBase>(new(strategyId));
    //     strategy.ClosePosition(request.Symbol, clock.GetCurrentInstant(), request.StrategyPositionId,
    //         request.Price, request.ClOrdId);
    //     await persistence.CommitAsync();
    //     return Ok();
    // }
    //
    // // [HttpGet, Route("{strategyId:guid}/realtime-execution")]
    // // [EndpointName(nameof(GetEsaPositions))]
    // // [Produces("application/json")]
    // // public async Task GetEsaPositions([FromRoute] Guid strategyId)
    // // {
    // //     context.SignalGroups
    // //         .Include(sg => sg.AccountsSignalGroups)
    // //         .ThenInclude(asg => asg.Allocations)
    // //         .Include(sg => sg.AccountsSignalGroups)
    // //         .Where(sg => sg.StrategyId == strategyId)
    // //         .AsNoTracking()
    // //         
    // // }
    //
    // [HttpGet, Route("execution-stats")]
    // [EndpointName(nameof(GetStrategyExecutionStats))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<StrategyExecutionStatsModel>> GetStrategyExecutionStats([FromQuery] StrategyExecutionStatsFilter? filter = null)
    // {
    //     filter ??= new();
    //     
    //     var res = await context.StrategyExecutionStats
    //         .Include(s => s.Account)
    //         .Include(s => s.Strategy)
    //         .Include(s => s.Contract)
    //         .Where(s => (filter.StrategyId == null || filter.StrategyId == s.StrategyId)
    //             && (filter.AccountId == null || filter.AccountId == s.AccountId)
    //             && (filter.ContractId == null || filter.ContractId == s.ContractId)
    //             && (filter.FromDt == null || filter.FromDt <= s.CreatedAt)
    //             && (filter.ToDt == null || filter.ToDt >= s.CreatedAt)
    //         )
    //         .GroupBy(s => new { s.AccountId, s.Account.Name, s.ContractId, s.Contract.Ticker })
    //         .Select(gr => 
    //             new StrategyExecutionStatsModel
    //         {
    //             ContractId = gr.Key.ContractId,
    //             Ticker = gr.Key.Ticker,
    //             Account = new(gr.Key.AccountId, AccountType.ExecutableSubAccount, gr.Key.Name),
    //             ExecutionTime = Duration.FromSeconds(Math.Round(gr.Average(s => s.ExecutionTime.TotalSeconds), 2)),
    //             FillRate = Math.Round(gr.Average(s => s.FillRate), 6),
    //             StrategySlippageRate = Math.Round(gr.Average(s => s.StrategySlippageRate), 6),
    //             StrategyCommissionRate = Math.Round(gr.Average(s => s.StrategyCommissionRate), 6),
    //             SlippageRate = Math.Round(gr.Average(s => s.SlippageRate), 6),
    //             CommissionRate = Math.Round(gr.Average(s => s.CommissionRate), 6)
    //         })
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     return res;
    // }
    //
    // [HttpGet, Route("virtual-executor-orders")]
    // [EndpointName(nameof(GetVirtualExecutorOrders))]
    // public async Task<IEnumerable<OrderStatus>> GetVirtualExecutorOrders() =>
    //     await _serviceWrapper.StrategiesServiceClient.GetVirtualExecutorOrdersAsync();
    //
    // [HttpGet, Route("execution-summary")]
    // [EndpointName(nameof(GetExecutionConfigurationSummary))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<StrategyExecutionViewItem>> GetExecutionConfigurationSummary()
    // {
    //     var strategies = (await GetStrategies(new() { Status = true })).ToList();
    //     
    //     Dictionary<Guid, List<AccountViewBriefWithInvestment>> executionAccounts = new(strategies.Count);
    //     foreach (var strategy in strategies)
    //     {
    //         executionAccounts[strategy.StrategyId] = (await GetExecutionAccountsForStrategy(strategy.StrategyId)).ToList();
    //     }
    //     
    //     // TODO: add execution policy description
    //     
    //     var res = strategies.Select(s => new StrategyExecutionViewItem(
    //         s.StrategyId, s.Name, s.StrategyClassName,
    //         s.Symbols.Values.ToList(),
    //         s.Account,
    //         executionAccounts[s.StrategyId]
    //     )).ToList();
    //     
    //     return res;
    // }
    //
    // [HttpGet, Route("ssa-to-broker-accounts-summary")]
    // [EndpointName(nameof(GetSsasToBrokerAccountsMappingSummary))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<StrategyAccountToBrokerAccountItemView>> GetSsasToBrokerAccountsMappingSummary([FromQuery] SsasToBrokerAccountsMappingSummaryFilter? filter = null)
    // {
    //     filter ??= new();
    //     
    //     var accounts = await context.Accounts
    //         .Include(a => a.Strategy)
    //         .Include(a => a.EsaSubscription)
    //             .ThenInclude(s => s.Strategy)
    //         .Include(a => a.Subaccounts.Where(sa => sa.Classifier == IStrategySubaccountState.BrokerAccountClassifier))
    //             .ThenInclude(sa => sa.Subaccount)
    //             .ThenInclude(a => a.TradingClient)
    //         .Where(a => 
    //             (
    //                 a.AccountType == AccountType.ExecutableSubAccount
    //                 && a.EsaSubscription != null 
    //                 && (!filter.OnlyActiveStrategies || a.EsaSubscription.Strategy.IsActive) 
    //             ) || 
    //             (
    //                 a.AccountType == AccountType.StrategySubAccount
    //                 && (!filter.OnlyActiveStrategies || a.Strategy!.IsActive)
    //             )
    //         )
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     var strategies = accounts
    //         .Select(a => a.AccountType == AccountType.StrategySubAccount ? a.Strategy : a.EsaSubscription!.Strategy)
    //         .ToList();
    //     
    //     var contractIds = strategies.SelectMany(s => s!.Symbols.Values).Distinct().ToHashSet();
    //
    //     var contracts = await context.Contracts
    //         .Include(c => c.Template)
    //             .ThenInclude(t => t.Broker)
    //         .Include(c => c.SyntheticCompositionHistory)
    //             .ThenInclude(h => h.CompositionWeights)
    //         .Where(c => contractIds.Contains(c.ContractId))
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     var synthContracts = contracts
    //         .Where(c => c.IsSynthetic())
    //         .Select(c => new Contract(c, c.Template, null, null, null, null, 
    //             null, null, null, null, 
    //             c.SyntheticCompositionHistory.Select(comp => comp.ToComposition()).ToList())
    //         ).ToList();
    //
    //     IEnumerable<Tuple<long, long>> synthUnderlyingContractIds;
    //     if (filter.IncludeSynthComposition == IncludeSynthComposition.Current || filter.IncludeSynthComposition == IncludeSynthComposition.CurrentAndNext)
    //     {
    //         var now = clock.GetCurrentInstant();
    //         synthUnderlyingContractIds = synthContracts.SelectMany(c =>
    //             c.GetCurrentSyntheticContractComposition(now)?.Weights
    //                 .Select(w => new Tuple<long, long>(c.ContractId, w.Key)) 
    //                 ?? Array.Empty<Tuple<long, long>>()
    //         );
    //
    //         if (filter.IncludeSynthComposition == IncludeSynthComposition.CurrentAndNext)
    //         {
    //             synthUnderlyingContractIds = synthUnderlyingContractIds.Union(synthContracts.SelectMany(c =>
    //                 c.GetNextSyntheticContractComposition(now)?.Weights.Select(w => new Tuple<long, long>(c.ContractId, w.Key)) 
    //                 ?? Array.Empty<Tuple<long, long>>()
    //             ));
    //         }
    //     }
    //     else
    //     {
    //         synthUnderlyingContractIds = synthContracts.SelectMany(c =>
    //             c.SyntheticContractCompositionHistory?.SelectMany(h => h.Weights.Select(w => new Tuple<long, long>(c.ContractId, w.Key))) 
    //             ?? Array.Empty<Tuple<long, long>>()
    //         );
    //     }
    //
    //     var underlyingContracts = synthUnderlyingContractIds.ToList();
    //     var underlyingContractIds = underlyingContracts.Select(i => i.Item2).Distinct().Except(contractIds).ToHashSet();
    //     contracts.AddRange(
    //         await context.Contracts
    //             .Include(c => c.Template)
    //                 .ThenInclude(t => t.Broker)
    //             .Where(c => underlyingContractIds.Contains(c.ContractId))
    //             .AsNoTracking()
    //             .ToListAsync()
    //     );
    //     var contractsDict = contracts.ToDictionary(k => k.ContractId);
    //     var synths = underlyingContracts.GroupBy(t => t.Item1)
    //         .ToDictionary(gr => gr.Key, gr => gr.Select(t => t.Item2).ToList());
    //
    //     var res = accounts.SelectMany(a =>
    //     {
    //         var strategy = a.AccountType == AccountType.StrategySubAccount ? a.Strategy : a.EsaSubscription!.Strategy;
    //         var acc = new AccountViewBrief(a.AccountId, a.AccountType, a.Name);
    //
    //         var strategySynths = new List<long>();
    //         var res = strategy!.Symbols.Values.Select(s => GetDataForContract(a, contractsDict[s], strategySynths,
    //             strategy, acc, null)).ToList();
    //
    //         foreach (var cid in strategySynths)
    //         {
    //             var synthView = new BriefView<long>(cid, contractsDict[cid].Ticker);
    //             res.AddRange(synths[cid].Select(und =>
    //                 GetDataForContract(a, contractsDict[und], strategySynths, strategy, acc, synthView)));
    //         }
    //
    //         return res;
    //     });
    //
    //     return res;
    // }
    //
    // private StrategyAccountToBrokerAccountItemView GetDataForContract(Account account, Databases.Main.Models.Contracts.Contract contract, List<long> synths,
    //     Strategy strategy, AccountViewBrief acc, BriefView<long>? synthSymbol)
    // {
    //     if (contract.IsSynthetic()) synths.Add(contract.ContractId);
    //
    //     var broker = contract.Template.BrokerId.HasValue
    //         ? new BriefView<long>(contract.Template.BrokerId.Value, contract.Template.Broker.Name)
    //         : default(BriefView<long>?);
    //
    //     AccountViewBrief? brokerAccount = null;
    //     var externalAccountId = string.Empty;
    //     if (broker != null)
    //     {
    //         var ba = account.Subaccounts.SingleOrDefault(sa => sa.CorrelationEntityId == broker.Value.Id);
    //         if (ba != null)
    //         {
    //             brokerAccount = new(ba.Subaccount.AccountId, ba.Subaccount.AccountType, ba.Subaccount.Name);
    //             externalAccountId = ba.Subaccount.TradingClient?.ExternalAccountId;
    //         }
    //     }
    //
    //     return new StrategyAccountToBrokerAccountItemView(
    //         strategy.StrategyId,
    //         strategy.Name,
    //         strategy.ClassName,
    //         acc,
    //         contract.ContractId, contract.Ticker,
    //         broker?.Id,
    //         broker?.Name,
    //         brokerAccount, 
    //         synthSymbol?.Id,
    //         synthSymbol?.Name, 
    //         externalAccountId
    //     );
    // }
}