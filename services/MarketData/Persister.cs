using System.Collections.Generic;
using Common.Metrics;
using Disruptor;
using NodaTime;
using Prometheus;
using QuantInfra.Common.MarketData.Infrastructure;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.MarketData;

public class Persister : IEventHandler<OutgoingDisruptorMessage>
{
    private readonly IMarketDataPersister _persister;
    private readonly Dictionary<int, Instant> _lastPersistedOpenDts = new();
    private readonly Counter? _persistedMessages;
    private readonly Counter? _numberOfCommits;
    private readonly Histogram? _persistTime;

    public Persister(Config config, IMarketDataPersister persister)
    {
        _persister = persister;

        if (config.WritePerformanceMetrics)
        {
            _persistedMessages = SharedMetricsDefinition.GetPersistedMessages(config.MarketDataServiceName, config.Monolith);
            _numberOfCommits = SharedMetricsDefinition.GetNumberOfCommits(config.MarketDataServiceName, config.Monolith);
            _persistTime = SharedMetricsDefinition.GetPersistTime(config.MarketDataServiceName, config.Monolith,
                config.PersistTimeParams[0], config.PersistTimeParams[1], config.PersistTimeParams[2]);
        }
    }
    
    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;

        if (data.Value is Candle1MClosedEvt candle)
        {
            var streamId = candle.Bar.StreamId;
            if (!_lastPersistedOpenDts.TryGetValue(streamId, out var lastPersistedOpenDt))
            {
                lastPersistedOpenDt = _persister.GetLastPersistedOpenDt(streamId);
                _lastPersistedOpenDts[streamId] = lastPersistedOpenDt;
            }
            if (candle.Bar.OpenDt <= lastPersistedOpenDt) return;
            
            var start = MetricsUtils.GetUnixMicro();
            _persister.AppendBAU(candle.Bar, BarAggregationType.Time);
            var elapsed = MetricsUtils.GetUnixMicro() - start;
            
            _persistedMessages?.Inc();
            _numberOfCommits?.Inc();
            _persistTime?.Observe(elapsed);
        }
    }
}