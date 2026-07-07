using Common.StaticData.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Services.Api.Backtesting;

[ApiController]
[Route("api/results")]
public class ResultsController(ITestUnitsRepository unitsRepository, ITestResultsRepositoryReadonly repository, IStaticDataRepositoryReadOnly? sdRepository) : Controller
{
    [HttpGet("{unitId:guid}/strategies")]
    [EndpointName(nameof(GetStrategies))]
    [Produces("application/json")]
    public Task<IReadOnlyCollection<StrategyConfig>> GetStrategies([FromRoute] Guid unitId) => repository.GetStrategies(unitId); 
    
    [HttpGet("{unitId:guid}/returns")]
    [EndpointName(nameof(GetReturns))]
    [Produces("application/json")]
    public async Task<IEnumerable<SharePriceHistory>> GetReturns(
        [FromRoute] Guid unitId, [FromQuery] int? strategyId = null) => await repository.GetReturns(unitId, strategyId);
    
    [HttpGet("{unitId:guid}/trades")]
    [EndpointName(nameof(GetTrades))]
    [Produces("application/json")]
    public async Task<IEnumerable<TradeView>>? GetTrades(Guid unitId, [FromQuery] TradeFilter? filter = null)
    {
        var unit = await unitsRepository.GetTestUnitAsync(unitId);
        if (unit is null) return Array.Empty<TradeView>();
        
        var trades = await repository.GetTrades(unitId, filter ?? new());
        var contractIds = trades.Select(t => t.ContractId).Distinct().ToList();
        var tickers = await GetTickers(unit, contractIds);

        return trades.Select(t => new TradeView(t, string.Empty, tickers.GetValueOrDefault(t.ContractId, t.ContractId.ToString())!));
    }

    [HttpGet("{unitId:guid}/deals")]
    [EndpointName(nameof(GetPositionCloses))]
    [Produces("application/json")]
    public async Task<IEnumerable<PositionView>> GetPositionCloses(Guid unitId, [FromQuery] PositionHistoryFilter? filter = null)
    {
        var unit = await unitsRepository.GetTestUnitAsync(unitId);
        if (unit is null) return Array.Empty<PositionView>();
        
        var positions = await repository.GetPositions(unitId, filter ?? new());
        var contractIds = positions.Select(p => p.ContractId).Distinct().ToList();
        var tickers = await GetTickers(unit, contractIds);

        return positions.Select(p =>
            new PositionView(p, string.Empty, tickers.GetValueOrDefault(p.ContractId, p.ContractId.ToString())!, PositionChangeType.Close));
    }

    [HttpGet("{unitId:guid}/eod-balances")]
    [EndpointName(nameof(GetEndOfDayBalances))]
    [Produces("application/json")]
    public async Task<IEnumerable<BalanceValueView>> GetEndOfDayBalances(Guid unitId, [FromQuery] AccountEndOfDayBalancesFilter? filter = null)
    {
        var unit = await unitsRepository.GetTestUnitAsync(unitId);
        if (unit is null) return Array.Empty<BalanceValueView>();
        
        var values = await repository.GetEndOfDayBalances(unitId, filter ?? new());
        var currencyIds = values.Select(p => p.CurrencyId).Distinct().ToList();
        var currencies = sdRepository is not null
            ? (await sdRepository.GetCurrenciesAsync(currencyIds)).ToDictionary(c => c.CurrencyId, c => c.Asset.Name)
            : new Dictionary<int, string>();

        return values.Select(v => new BalanceValueView(v, currencies.GetValueOrDefault(v.CurrencyId, v.CurrencyId.ToString())));
    }

    [HttpGet("{unitId:guid}/metrics")]
    [EndpointName(nameof(GetMetrics))]
    [Produces("application/json")]
    public Task<MetricsTable?> GetMetrics(Guid unitId)
    {
        return repository.GetMetrics(unitId);
    }

    // [HttpGet("{unitId:guid}/end-of-day")]
    // [EndpointName(nameof(GetEndOfDayPositions))]
    // [Produces("application/json")]
    // public async Task<IEnumerable<(Position, PositionValue)>> GetEndOfDayPositions([FromRoute] Guid unitId, [FromQuery] AccountEndOfDayPositionsFilter? filter = null)
    // {
    //     var unit = await unitsRepository.GetTestUnitAsync(unitId);
    //     if (unit is null) return Array.Empty<Position>();
    //     
    //     var positions = await repository.GetEndOfDayPositions(unitId, filter ?? new());
    //     var contractIds = positions.Select(p => p.Item1?.ContractId).Distinct().ToList();
    //     var tickers = await GetTickers(unit, contractIds);
    //
    //     return positions.Select(p =>
    //         new PositionView(p, string.Empty, tickers.GetValueOrDefault(p.ContractId)!, PositionChangeType.Close));
    // }

    private async Task<IReadOnlyDictionary<int, string>> GetTickers(TestUnit unit, IEnumerable<int> contractIds)
    {
        var enumerated = contractIds.ToList();
        if (enumerated.Any() && sdRepository is not null)
        {
            var contracts = await sdRepository.GetContractsAsync(new()
            {
                ContractIds = enumerated,
                Limit = -1,
            });
            return contracts.ToDictionary(c => c.ContractId, c => c.Ticker);
        }
        return new Dictionary<int, string>();
    }
}