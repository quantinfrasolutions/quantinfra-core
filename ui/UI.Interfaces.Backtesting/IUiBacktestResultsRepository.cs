using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.UI.Interfaces.Backtesting;

public interface IUiBacktestResultsRepository
{
    Task<IEnumerable<StrategyConfig>> GetStrategies(Guid testUnitId);
    Task<IEnumerable<SharePriceHistory>> GetReturns(Guid testUnitId, int? strategyId = null);
    Task<IEnumerable<PositionView>> GetPositionCloses(Guid testUnitId, PositionHistoryFilter? filter = null);
    Task<IEnumerable<BalanceValueView>> GetEndOfDayBalances(Guid testUnitId, AccountEndOfDayBalancesFilter? filter = null);
    Task<IEnumerable<PositionValueView>> GetEndOfDayPositions(Guid testUnitId, AccountEndOfDayBalancesFilter? filter = null);
    Task<IEnumerable<TradeView>> GetTrades(Guid testUnitId, TradeFilter? filter = null);
    Task<MetricsTable> GetMetrics(Guid testUnitId);
}