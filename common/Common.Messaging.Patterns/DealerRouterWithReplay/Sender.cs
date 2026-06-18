using System;
using Disruptor;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public class Sender : IEventHandler<OutgoingDisruptorMessage>
{
    private readonly ITransport<DownstreamMessage> _transport;
    private readonly IDealerRouterMessageFactory _messageFactory;
    private readonly ILogger _logger;
    private readonly Disruptor<OutgoingDisruptorMessage> _disruptor;
    private readonly IClock _clock;
    
    private long _transportSeqNumOffset = 0;
    private long _sessionId;
    
    public long SessionId => _sessionId;
    
    public Sender(
        ITransport<DownstreamMessage> transport,
        IDealerRouterMessageFactory messageFactory,
        ILogger<Sender> logger, 
        Disruptor<OutgoingDisruptorMessage> disruptor, 
        IClock clock
    )
    {
        _transport = transport;
        _messageFactory = messageFactory;
        _logger = logger;
        _disruptor = disruptor;
        _clock = clock;
    }

    public void Start()
    {
        _sessionId = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        _logger.LogInformation($"Starting sender with session id {_sessionId}");
        _disruptor.PublishMessage(_messageFactory.CreateSessionStartMessage(_sessionId));
    }
    
    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        OnBeforeHandle?.Invoke(data);
        if (data.Value is DownstreamMessage msg)
        {
            switch (msg.MessageType)
            {
                case MessageType.SessionStart:
                    _transportSeqNumOffset = sequence;
                    _transport.SendMessage(_messageFactory.CreateSessionStartMessage(_sessionId));
                    break;
            }
            
            return;
        }

        if (data.Value is ControlMessage ctl)
        {
            switch (ctl.MessageType)
            {
                case MessageType.FillGap:
                    // TODO: check for overflow and send SequenceReset in case of there is not enough messages in the buffer
                    
                    // TODO: if ctl.SessionId != _sessionId
                    
                    var buffer = _disruptor.RingBuffer;
                    _logger.LogInformation($"Replaying messages starting from {ctl.Sequence}");
                    var disruptorSeq = ctl.Sequence - _transportSeqNumOffset;
                    
                    var oldestRetainedSeq = Math.Max(_disruptor.Cursor - _disruptor.RingBuffer.BufferSize, 0);
                    var sendFillGap = false;
                    if (oldestRetainedSeq > disruptorSeq)
                    {
                        _logger.LogInformation($"Not enough data to replay all requested messages, disruptorSeq={disruptorSeq}, oldestRetainedSeq={oldestRetainedSeq}");
                        disruptorSeq = oldestRetainedSeq;
                        sendFillGap = true;
                    }
                    for (var i = disruptorSeq; i < sequence; i++)
                    {
                        var replayedMsg = buffer[i];
                        if (replayedMsg.Value is DownstreamMessage { MessageType: MessageType.SessionStart } ss)
                        {
                            _transport.SendMessage(ss);
                        }
                        else
                        {
                            // var payload = replayedMsg.Payload;
                            // if (payload == null) continue; // ???
                            if (sendFillGap)
                            {
                                // Wait until the first message to be sent and indicate that the earlier messages will not be available
                                _transport.SendMessage(_messageFactory.CreateSequenceResetMessage(_sessionId, replayedMsg.TransportSequence));
                                sendFillGap = false;
                            }
                            _transport.SendMessage(_messageFactory.CreateDataMessage(_sessionId, replayedMsg.TransportSequence, replayedMsg.Value));
                        }
                    }

                    _transportSeqNumOffset++;
                    return;
            }
        }

        if (_sessionId == 0) return;
        
        // var serialized = _messageFactory.SerializeAsString(data.Value);
        // data.Payload = serialized;
        data.TransportSequence = sequence - _transportSeqNumOffset;
        _logger.LogTrace("Sending message {sequence} with transport sequence {transportSequence}", sequence, data.TransportSequence);
        var message = _messageFactory.CreateDataMessage(_sessionId, sequence - _transportSeqNumOffset, data.Value);
        _transport.SendMessage(message);
        OnAfterHandle?.Invoke(data);
    }
    
    public Action<OutgoingDisruptorMessage>? OnBeforeHandle { get; set; }
    public Action<OutgoingDisruptorMessage>? OnAfterHandle { get; set; }
}