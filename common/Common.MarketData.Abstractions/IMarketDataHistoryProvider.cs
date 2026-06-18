using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData.Abstractions;

public interface IMarketDataHistoryProvider
{
    IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt);
        
    IEnumerable<ExchangeBar> GetBAUsByStream(
        int streamId,
        Instant from,
        Instant to
    );

    IEnumerable<ExchangeBar> GetBAUsByContract(
        int contractId,
        Instant from,
        Instant to
    );

    IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone);
    IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone);
}