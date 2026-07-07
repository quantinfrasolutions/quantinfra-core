using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface ITestResultsRepositoryReadonly
{
    Task<IReadOnlyCollection<StrategyConfig>> GetStrategies(Guid unitId);
    Task<IReadOnlyList<SharePriceHistory>> GetReturns(Guid unitId, int? strategyId = null);
    Task<IReadOnlyList<Position>> GetPositions(Guid testUnitId, PositionHistoryFilter filter);
    Task<IReadOnlyList<BalanceValue>> GetEndOfDayBalances(Guid unitId, AccountEndOfDayBalancesFilter filter);
    Task<IReadOnlyList<Trade>> GetTrades(Guid unitId, TradeFilter filter);
    Task<IReadOnlyList<(Position, PositionValue)>> GetEndOfDayPositions(Guid unitId, AccountEndOfDayPositionsFilter filter);
    Task<MetricsTable?> GetMetrics(Guid unitId);
}