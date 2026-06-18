using System;
using System.Collections.Generic;
using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

/// <summary>
/// This class controls the correct sequence of messages received by the Router socket.
/// Upon the service startup and restoring the state, the current sessions must be initialized by the state keeper.
/// The filter doesn't allow messages with the wrong session or sequence number into the incoming disruptor. 
/// When a message is received by the socket, it is checked against the expected session number:
///     * if the session differs from the expected session and is not the message is not a session start message,
///         the local session state is reset and the fill gap is requested immediately. The message is not passed
///         to the input disruptor (so the persisted state remains unchanged).
///     * if the session is correct, but the sequence number is less or equal to the last received sequence, the message
///         is simply ignored and not placed to the input disruptor.
///     * if the session is correct, but the sequence number is greater than expected, the FillGap is sent immediately.
///         All subsequent messages are ignored until SessionStart or FillGap is received.
///     * if the session and sequence are correct, the internal sequence is updated and the message is placed to the disruptor.
/// The BPL must:
///     * Initialize the state upon finishing hydration
///     * Update the sessions stored in the state by calling the respective method of the ReceiverFilter
///         (this is done in the BPL thread) 
/// </summary>

public class ReceiverFilter
{
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly ITransport<ControlMessage> _transport;
    private readonly IReceiverStateProvider _stateProvider;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private readonly bool _enableTransactionLogging = false;

    private ReceiverState? _state;
    
    public ReceiverFilter(
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        ITransport<ControlMessage> transport,
        ILogger<ReceiverFilter> logger, 
        IReceiverStateProvider stateProvider, 
        IClock clock
    )
    {
        _inputDisruptor = inputDisruptor;
        _transport = transport;
        _logger = logger;
        _stateProvider = stateProvider;
        _clock = clock;
    }

    public void HandleIncomingMessage(DownstreamMessage msg)
    {
        if (_state == null) throw new InvalidOperationException("State is not initialized");
        
        var swMicro = MetricsUtils.GetUnixMicro();
        var receivedAt = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        
        if (msg.MessageType == MessageType.SessionStart || msg.MessageType == MessageType.SequenceReset)
        {
            _logger.LogInformation($"{msg.MessageType}, senderCompId={msg.SenderCompId}, sessionId={msg.SessionId}, sequenceNumber={msg.SequenceNumber}");
            _state.SetSession(msg.SenderCompId, new UpstreamSession(msg.SessionId, msg.SequenceNumber));
            _inputDisruptor.PublishMessage(msg, receivedAt, swMicro);
        }
        else if (msg.MessageType == MessageType.DataMessage)
        {
            var session = _state.Sessions.GetValueOrDefault(msg.SenderCompId);
            
            if (session == null)
            {
                _logger.LogInformation($"Session doesn't exist, requesting fill gap for {msg.SenderCompId}, sessionId={msg.SessionId}");
                _state.SetSession(msg.SenderCompId, new UpstreamSession(msg.SessionId, 0) { RequestedFillGaps = 0 });
                _transport.SendMessage(ControlMessage.FillGap(msg.SenderCompId, msg.SessionId, 0));
                return;
            }

            if (session.SessionId != msg.SessionId)
            {
                _logger.LogInformation($"Session doesn't match, requesting fill gap for {msg.SenderCompId}, sessionId={msg.SessionId}");
                _state.SetSession(msg.SenderCompId, new UpstreamSession(msg.SessionId, 0) { RequestedFillGaps = 0 });
                _transport.SendMessage(ControlMessage.FillGap(msg.SenderCompId, msg.SessionId, 0));
                return;
            }

            if (msg.SequenceNumber <= session.SequenceNumber) return;
            
            var expectedSeq = session.SequenceNumber + 1;

            if (msg.SequenceNumber == expectedSeq)
            {
                if (_enableTransactionLogging) _logger.LogDebug($"Received message {msg.SequenceNumber} for {msg.SenderCompId} and session {msg.SessionId}, sending to input disruptor");
                _inputDisruptor.PublishMessage(msg, receivedAt, swMicro);
                if (session.RequestedFillGaps.HasValue) _logger.LogInformation($"Fill gap for {msg.SenderCompId} completed");
                session.RequestedFillGaps = null;
                session.SequenceNumber++;
                return;
            }

            if (session.RequestedFillGaps.HasValue && session.RequestedFillGaps.Value <= expectedSeq) return;
            
            _logger.LogWarning($"Received message {msg.SequenceNumber} for {msg.SenderCompId} and session {msg.SessionId}, requesting fill gap from {session.SequenceNumber}");
            session.RequestedFillGaps = expectedSeq;
            _transport.SendMessage(ControlMessage.FillGap(msg.SenderCompId, msg.SessionId, expectedSeq));
        }
    }

    public void InitializeState()
    {
        _state = _stateProvider.GetReceiverState().Copy();
        _logger.LogInformation($"Initialized state: {_state}");
    }

    public void UpdateState(string senderCompId, long sessionId, long sequenceNumber) =>
        _stateProvider.UpdateState(senderCompId, sessionId, sequenceNumber);
}