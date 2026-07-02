using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.UI.Interfaces.Backtesting;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

public partial class ApiRepository : IUiBacktestResultsRepository
{
    public Task<IEnumerable<StrategyConfig>> GetStrategies(Guid testUnitId) => RetrieveCollection(
        "strategies", () => _wrapper.Client.GetStrategiesAsync(testUnitId));

    public Task<IEnumerable<SharePriceHistory>> GetReturns(Guid testUnitId, int? strategyId = null) =>
        Retrieve("returns", () => _wrapper.Client.GetReturnsAsync(testUnitId, strategyId));

    public Task<IEnumerable<PositionView>> GetPositionCloses(Guid testUnitId, PositionHistoryFilter? filter = null) =>
        RetrieveCollection("deals", () => _wrapper.Client.GetPositionClosesAsync(testUnitId, filter?.CloseDtFrom, filter?.CloseDtTo,
            null, filter?.OpenDtFrom, filter?.OpenDtTo, filter?.HistoryOpenDtFrom, filter?.HistoryOpenDtTo, filter?.AccountId, filter?.ContractId, filter?.TradeId,
            filter?.Limit, filter?.Offset));

    public Task<IEnumerable<BalanceValueView>> GetEndOfDayBalances(Guid testUnitId, AccountEndOfDayBalancesFilter? filter = null) =>
        RetrieveCollection("balance values", () => _wrapper.Client.GetEndOfDayBalancesAsync(testUnitId, filter?.AccountId,
            filter?.FromDt, filter?.ToDt, filter?.Limit, filter?.Offset));

    public Task<IEnumerable<PositionValueView>> GetEndOfDayPositions(Guid testUnitId, AccountEndOfDayBalancesFilter? filter = null) =>
        throw new NotImplementedException();
        // RetrieveCollection("end of day positions", () => _wrapper.Client.GetPo)

    public Task<IEnumerable<TradeView>>? GetTrades(Guid testUnitId, TradeFilter? filter = null) =>
        RetrieveCollection("trades", () => _wrapper.Client.GetTradesAsync(testUnitId, filter?.FromDt, filter?.ToDt,
            filter?.AccountId, filter?.TradeId, filter?.ContractId, filter?.ExternalId, filter?.Limit, filter?.Offset));
}