using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData;

public class AggregatingBar
{
    public AggregatingBar(int streamId, int? contractId, Instant openDt, Instant closeDt, double open, double high, double low, double close, double volume, double dollarValue, int datasourceId, int? tradingSessionId)
    {
        StreamId = streamId;
        ContractId = contractId;
        OpenDt = openDt;
        CloseDt = closeDt;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        DollarValue = dollarValue;
        DatasourceId = datasourceId;
        TradingSessionId = tradingSessionId;
    }

    public int StreamId { get; init; }
    public int? ContractId { get; init; }
    public Instant OpenDt { get; init; }
    public Instant CloseDt { get; set; }
    public double Open { get; init; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
    public double DollarValue { get; set; }
    public int DatasourceId { get; init; }
    public int? TradingSessionId { get; init; }

    public ExchangeBar ToExchangeBar() => new(StreamId, ContractId, OpenDt, CloseDt, Open, High, Low, Close, 
        Volume, DollarValue, DatasourceId, TradingSessionId);
}