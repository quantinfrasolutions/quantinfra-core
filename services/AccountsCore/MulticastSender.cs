using System;
using System.Collections.Generic;
using System.Threading;
using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.ServiceMessages;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies.AccountsService;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Services.AccountsCore;

public class MulticastSender : Disruptor.IEventHandler<OutgoingDisruptorMessage>
{
    private readonly IClock _clock;
    private readonly ITransport<DownstreamMessage> _transport;
    private readonly IOutputToInputDisruptorPublisher _publisher;
    private readonly IMulticastMessageFactory _multicastMessageFactory;
    private readonly ILogger _logger;
    private readonly string _serviceName;
    
    private readonly Dictionary<string, long> _sessions = new();
    private readonly Dictionary<long, Guid> _accountStateRequests = new();
    private readonly Dictionary<long, Guid> _strategyStateRequests = new();
    
    private readonly bool _writePerformanceMetrics;
    private readonly Histogram? _totalProcessingTime;
    private readonly Counter? _downstreamSenderMessages;
    
    private long _lastSentEventId = -1;
    private long _heartbeatsSequenceNumber = -1;

    public MulticastSender(
        Config config,
        IClock clock,
        ITransport<DownstreamMessage> transport,
        IOutputToInputDisruptorPublisher publisher,
        IMulticastMessageFactory multicastMessageFactory,
        ILogger<MulticastSender> logger
    )
    {
        _serviceName = config.AccountServiceName;
        _clock = clock;
        _transport = transport;
        _publisher = publisher;
        _multicastMessageFactory = multicastMessageFactory;
        _logger = logger;

        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _totalProcessingTime = SharedMetricsDefinition.TotalProcessingTime;
            _downstreamSenderMessages = SharedMetricsDefinition.DownstreamSenderMessages;
        }
    }
    
    public SemaphoreSlim StopSemaphore { get; } = new(0, 1);
    
    public void UpdateLastSentEventId(long lastSentEventId)
    {
        _lastSentEventId = lastSentEventId;
    }

    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Value is StopEvt)
        {
            StopSemaphore.Release();
            return;
        }
        
        if (_lastSentEventId < 0) throw new InvalidOperationException("DownstreamFilter not started");

        _downstreamSenderMessages?.Inc();
        
        if (data.Value is IEvent e)
        {
            _logger.LogTrace("Processing event {eventId}", e.EventId);
            
            if (data.Value is AccountsServiceHeartbeatEvt hb)
            {
                var topic = TopicDefinitions.HeartbeatsTopic;
                if (!_sessions.TryGetValue(topic, out var sessionId))
                {
                    sessionId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
                    _sessions.Add(topic, sessionId);
                    _transport.SendMessage(_multicastMessageFactory.CreateSessionStartMessage(_serviceName, topic, sessionId, 0, null));
                    _heartbeatsSequenceNumber = 0;
                }

                var seqNo = ++_heartbeatsSequenceNumber;
                data.Value = new AccountsServiceHeartbeatEvt(seqNo, hb.Timestamp);
                _transport.SendMessage(_multicastMessageFactory.CreateDataMessage(_serviceName, topic, sessionId, seqNo, data.Value));

                return;
            }
            
            if (e.EventId <= _lastSentEventId)
            {
                data.Skip = true;
                return;
            }

            var expectedEventId = _lastSentEventId + 1;
            if (e.EventId != expectedEventId) 
                throw new InvalidOperationException($"Expected event id {expectedEventId}, got {e.EventId}");
            
            _lastSentEventId = e.EventId;
        }

        bool recordTotalProcessingTime = _writePerformanceMetrics && data.SwReceivedAt != 0;
        
        if (data.Value is AsyncQueryResponse<GetAccountState, AccountStateReadonly?>
            || data.Value is AsyncQueryResponse<GetBrokerAccountState, BrokerAccountStateReadonly?>)
        {
            // var payload = Serialize(data);
            Guid reqId = Guid.Empty;
            bool useMulticast = false;
            AccountStateReadonly? result = null; 
            switch (data.Value) 
            {
                case AsyncQueryResponse<GetAccountState, AccountStateReadonly?> accountState:
                    reqId = accountState.RequestId;
                    result = accountState.Result;
                    useMulticast = accountState.UseMulticast;
                    break;
                case AsyncQueryResponse<GetBrokerAccountState, BrokerAccountStateReadonly?> brokerAccountState:
                    reqId = brokerAccountState.RequestId;
                    result = brokerAccountState.Result;
                    useMulticast = brokerAccountState.UseMulticast;
                    break;
            };
                
            if (result != null)
            {
                var accountId = result.AccountId;
                var topic = TopicDefinitions.GetAccountUpdatesTopic(result.AccountId);
                
                // If there is a request for this account, start a session (even if the state is a response to another request) and 
                // send the response to the response topic.
                // Else, just send the response.
                if (_accountStateRequests.Remove(accountId, out var requestId) || !_sessions.ContainsKey(topic) || useMulticast)
                {
                    var sessionId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
                    _logger.LogDebug($"Session started for topic {topic} with id {sessionId}, version={result.Version}");
                    _sessions[topic] = sessionId;
                    _transport.SendMessage(_multicastMessageFactory.CreateSessionStartMessage(_serviceName, topic, sessionId, result.Version, data.Value));
                }
            }

            _logger.LogDebug($"Sending response for request {reqId}");
            _transport.SendMessage(_multicastMessageFactory.CreateResponseMessage(_serviceName, data.Value));
        }
        else if (data.Value is AsyncQueryResponse<GetStrategyState, StrategyStateReadonly?> strategyState)
        {
            // var payload = Serialize(data);
            if (strategyState.Result != null)
            {
                var strategyId = strategyState.Result.StrategyId;
                var topic = TopicDefinitions.GetStrategyUpdatesTopic(strategyState.Result.StrategyId);
                
                // If there is a request for this strategy, start a session (even if the state is a response to another request) and 
                // send the response to the response topic.
                // Else, just send the response.
                if (_strategyStateRequests.Remove(strategyId, out var requestId) || !_sessions.ContainsKey(topic) || strategyState.UseMulticast)
                {
                    var sessionId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
                    _logger.LogDebug($"Session started for topic {topic} with id {sessionId}, version={strategyState.Result.Version}");
                    _sessions[topic] = sessionId;
                    _transport.SendMessage(_multicastMessageFactory.CreateSessionStartMessage(_serviceName, topic, sessionId, strategyState.Result.Version, data.Value));
                }
            }

            _logger.LogDebug($"Sending response for request {strategyState.RequestId}");
            _transport.SendMessage(_multicastMessageFactory.CreateResponseMessage(_serviceName, data.Value));
        }
        else if (data.Value is AsyncQueryResponse)
        {
            _transport.SendMessage(_multicastMessageFactory.CreateResponseMessage(_serviceName, data.Value));
        }
        else if (data.Value is IAccountEventBase evt)
        {
            var accountId = evt.AccountId;
            var topic = TopicDefinitions.GetAccountUpdatesTopic(accountId);
            if (_sessions.TryGetValue(topic, out var sessionId))
            {
                _logger.LogTrace("Sending event, topic={topic}, eventId={eventId}, version={version}", topic, evt.EventId, evt.Version);
                _transport.SendMessage(_multicastMessageFactory.CreateDataMessage(_serviceName, topic, sessionId, evt.Version, data.Value));
            }
            else if (!_accountStateRequests.ContainsKey(accountId))
            {
                _logger.LogDebug($"Requesting snapshot for topic {topic}");
                var request = new GetAccountState(evt.AccountId, _serviceName);
                _publisher.PublishMessage("MulticastSender", request);
                _accountStateRequests[accountId] = request.RequestId;
                recordTotalProcessingTime = false;
            }

            if (evt is ExecutionReportEvt)
            {
                _transport.SendMessage(_multicastMessageFactory.CreateResponseMessage(_serviceName, data.Value));
            }

            if (evt is BalanceOperationProcessedEvt { RequestId: not null })
            {
                _transport.SendMessage(_multicastMessageFactory.CreateResponseMessage(_serviceName, data.Value));
            }
        }
        else if (data.Value is IStrategyEventBase se)
        {
            var strategyId = se.StrategyId;
            var topic = TopicDefinitions.GetStrategyUpdatesTopic(strategyId);
            if (_sessions.TryGetValue(topic, out var sessionId))
            {
                _logger.LogTrace("Sending event, topic={topic}, eventId={eventId}, version={version}", topic, se.EventId, se.Version);
                _transport.SendMessage(_multicastMessageFactory.CreateDataMessage(_serviceName, topic, sessionId, se.Version, data.Value));
            }
            else if (!_strategyStateRequests.ContainsKey(strategyId))
            {
                _logger.LogDebug($"Requesting snapshot for topic {topic}");
                var request = new GetStrategyState(strategyId);
                _publisher.PublishMessage("MulticastSender", request);
                _strategyStateRequests[strategyId] = request.RequestId;
                recordTotalProcessingTime = false;
            }
        }

        if (recordTotalProcessingTime)
        {
            _totalProcessingTime!.Observe(MetricsUtils.GetUnixMicro() - data.SwReceivedAt);
        }
    }
}