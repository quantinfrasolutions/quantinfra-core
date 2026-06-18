using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.Interfaces.Api.MarketData;

public class BarStorageView
{
    public BarStorageView() { }

    public BarStorageView(BarStorageConfig config, string name)
    {
        IdType = config.IdType;
        Id = config.Id;
        Ticker = name;
        AggregationType = config.AggregationType;
        TradingSessionIds = config.TradingSessionIds;
        Timeframe = config.Timeframe;
        Offset = config.Offset;
        Timezone = config.Timezone;
        LastValueOnly = config.LastValueOnly;
    }
    
    public IdType IdType { get; set; } = IdType.Contract;
    public int Id { get; set; }
    public string Ticker { get; set; }
    public BarAggregationType AggregationType { get; set; }
    public int[]? TradingSessionIds { get; set; }
    public Period Timeframe { get; set; } = Period.FromMinutes(1);
    public Period Offset { get; set; } = Period.Zero;
    public string Timezone { get; set; } = "UTC";
    public bool LastValueOnly { get; set; }

    public BarStorageView Copy() => new BarStorageView
    {
        IdType = IdType,
        Id = Id,
        Ticker = Ticker,
        AggregationType = AggregationType,
        TradingSessionIds = TradingSessionIds,
        Timeframe = Timeframe,
        Offset = Offset,
        Timezone = Timezone,
        LastValueOnly = LastValueOnly
    };

    public BarStorageConfig ToBarStorageConfig() => new()
    {
        IdType = IdType,
        Id = Id,
        AggregationType = AggregationType,
        TradingSessionIds = TradingSessionIds,
        Timeframe = Timeframe,
        Offset = Offset,
        Timezone = Timezone,
        LastValueOnly = LastValueOnly
    };
}