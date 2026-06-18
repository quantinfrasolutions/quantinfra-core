using System;
using System.Collections.Generic;
using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.MarketData;

public class MulticastSender : Disruptor.IEventHandler<OutgoingDisruptorMessage>
{
    private readonly IClock _clock;
    private ITransport<DownstreamMessage> _transport;
    private IMulticastMessageFactory _messageFactory;
    private readonly ILogger _logger;
    private readonly string _serviceName;
    private readonly bool _writePerformanceMetrics;
    
    private readonly Dictionary<string, long> _sessions = new();
    private readonly Dictionary<string, long> _sequences = new();
    private readonly Histogram? _totalTime;
    private readonly Histogram? _sendingDelay;
    private readonly Counter? _downstreamSenderMessages;

    public MulticastSender(
        Config config,
        IClock clock,
        ITransport<DownstreamMessage> transport,
        IMulticastMessageFactory messageFactory,
        ILogger<MulticastSender> logger
    )
    {
        _serviceName = config.MarketDataServiceName;
        _clock = clock;
        _transport = transport;
        _messageFactory = messageFactory;
        _logger = logger;
        
        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _sendingDelay = SharedMetricsDefinition.SendingDelay;
            _totalTime = SharedMetricsDefinition.TotalProcessingTime;
            _downstreamSenderMessages = SharedMetricsDefinition.DownstreamSenderMessages;
        }
    }

    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;
        
        _downstreamSenderMessages?.Inc();
        
        var now = MetricsUtils.GetUnixMicro();

        if (data.Value is AsyncQueryResponse<GetOrderBookSnapshot, OrderBookSnapshot?> obS)
        {
            if (obS.Result != null)
            {
                var t = TopicDefinitions.GetFullOrderBookTopic(obS.Result.ContractId);

                if (obS.UseMulticast || !_sessions.ContainsKey(t))
                {
                    var sId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
                    _transport.SendMessage(_messageFactory.CreateSessionStartMessage(_serviceName, t,
                        sId, 0, data.Value));
                    _sessions[t] = sId;
                    _sequences[t] = 1;
                }
            }
            
            _transport.SendMessage(_messageFactory.CreateResponseMessage(_serviceName, data.Value));
            return;
        }
        else if (data.Value is AggregatedOrderbookUpdateEvt obU)
        {
            var t = TopicDefinitions.GetFullOrderBookTopic(obU.ContractId);
            if (!_sessions.TryGetValue(t, out var sId))
            {
                return; // TODO: request snapshot
            }
            _transport.SendMessage(_messageFactory.CreateDataMessage(_serviceName, t,
                sId, _sequences[t],  data.Value));
            _sequences[t]++;
            return;
        }
        
        string topic = data.Value switch
        {
            Candle1MClosedEvt b => TopicDefinitions.GetCandles1mTopic(b.Bar.StreamId),
            // ExchangeTradeReceivedEvt t => $"s.ticks.{t.Trade.StreamId}",
            ContractLastPriceUpdatedEvt pu => TopicDefinitions.GetPriceUpdatesTopic(pu.ContractId),
            BestBidAskUpdatedEvt bbo => TopicDefinitions.GetBBOTopic(bbo.ContractId),
            _ => throw new ArgumentOutOfRangeException(),
        };
        
        if (!_sessions.TryGetValue(topic, out var sessionId))
        {
            sessionId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
            _sessions[topic] = sessionId;
            _sequences[topic] = 0;
            _transport.SendMessage(_messageFactory.CreateSessionStartMessage(_serviceName, topic, sessionId, 0, null));
        }
        _sequences[topic]++;
        _transport.SendMessage(_messageFactory.CreateDataMessage(_serviceName, topic, sessionId, _sequences[topic], data.Value));

        if (_writePerformanceMetrics)
        {
            var swReceivedAt = data.SwReceivedAt;
            if (swReceivedAt != 0) _totalTime!.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);

            var swPublished = data.SwPublishedAt;
            if (swReceivedAt != 0) _sendingDelay!.Observe(now - swPublished);
        }
    }
    
    // private string Serialize(OutgoingDisruptorMessage data)
    // {
    //     var serialized = _messageFactory.SerializeAsString(data.Value);
    //     data.Payload = serialized;
    //     return serialized;
    // }
}