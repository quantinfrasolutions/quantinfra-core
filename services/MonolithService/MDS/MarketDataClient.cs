using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Services.MonolithService.MDS;

public sealed class MarketDataClient :
    Listener,
    IMarketDataClient 
{
    private readonly Topology _topology;
    private readonly Disruptor<IncomingDisruptorMessage> _disruptor;
    private readonly ILogger<MarketDataClient> _logger;

    private readonly Dictionary<string, UpstreamSession?> _sessions = new();
    private readonly Dictionary<string, string> _sessionResetServerNames = new();
    private readonly HashSet<string> _snapshotRequests = new();
    

    public MarketDataClient(
        Topology topology, 
        TransportFactory factory,
        Disruptor<IncomingDisruptorMessage> disruptor, 
        IClock clock,
        ILogger<MarketDataClient> logger
    ) : base(disruptor, clock)
    {
        _topology = topology;
        _disruptor = disruptor;
        _logger = logger;
    }
    
    public Task RequestHistoricalBars(string exchangeSymbol, int count, int timeframe = 1)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToTicks(string exchangeSymbol)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToTicks(IEnumerable<string> exchangeSymbols)
    {
        throw new NotImplementedException();
    }

    public Task SubsribeToCandles1M(long streamId)
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetCandles1mTopic(streamId), this);
        return Task.CompletedTask;
    }

    public Task SubscribeToLastContractPricesAsync()
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetPriceUpdatesTopic(), this);
        return Task.CompletedTask;
    }

    public Task SubscribeToBestBidOffers(int contractId)
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetBBOTopic(contractId), this);
        return Task.CompletedTask;
    }

    public Task SubscribeToOrderBook(int contractId, string mdsName)
    {
        var topicName = TopicDefinitions.GetFullOrderBookTopic(contractId);
        RegisterSequenceController(topicName, mdsName);
        _topology.SubscribeToTopic(topicName, this);
        return Task.CompletedTask;
    }

    protected override bool CheckMessage(ITransportMessage message, string? topicName)
    {
        if (string.IsNullOrEmpty(topicName)) return true;                       // Not a multicast message
        if (!_sessions.TryGetValue(topicName, out var session)) return true;    // Topic with no session control
        
        if (message.MessageType == MessageType.SessionStart)
        {
            _logger.LogDebug("Session {sessionId} started for topic {topic}, sequenceId={sequenceId}", message.SessionId, topicName, message.SequenceNumber);
            session = new UpstreamSession(message.SessionId, message.SequenceNumber);
            _sessions[topicName] = session;
            _snapshotRequests.Remove(topicName);
            return true;
        }
        
        if (session == null || session.SequenceNumber < message.SequenceNumber)
        {
            _logger.LogWarning("Received sequence {sequence} for topic {message.TopicName}, expected {expected}",
                message.SequenceNumber, topicName, session?.SequenceNumber ?? 0);
            if (!_snapshotRequests.Contains(topicName))
            {
                _topology.SendRequestSnapshotMessage(new RequestSnapshotMessage(topicName), message.SenderCompId);
            }
            else
            {
                _logger.LogWarning($"Unknown senderCompId {message.SenderCompId}");
            }
            return false;
        }

        if (session.SequenceNumber > message.SequenceNumber)
        {
            _logger.LogWarning("Received sequence {sequenceNumber} for topic {topic}, expected {expected}, skipping",
                message.SequenceNumber, topicName, session.SequenceNumber);
            return false;
        }
            
        _logger.LogTrace("Received sequence {sequence} for topic {topic}", message.SequenceNumber, topicName);
        session.SequenceNumber = message.SequenceNumber + 1;
        return true;
    }

    private void RegisterSequenceController(string topicName, string serverName)
    {
        _sessions.TryAdd(topicName, null);
    }
}