// using Common.EventSourcing;
// using Common.Messaging;
// using Common.Metrics;
// using Disruptor.Dsl;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using NodaTime.Text;
// using QuantInfra.Common.ServiceBase;
//
// namespace QuanInfra.Common.ServiceBase.Finalizer;
//
// public sealed class Finalizer : Disruptor.IEventHandler<OutgoingDisruptorMessage>
// {
//     private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
//     private readonly IMessageFactory _messageFactory;
//     private readonly IClock _clock;
//     private readonly ILogger<Finalizer> _logger;
//     
//     private readonly long? _timeCutoff;
//     private readonly long? _eventIdCutoff;
//     private readonly bool _finalizeEveryMessage;
//     
//     private long _lastCutoffEventId;
//     private long _lastCutoffTime;
//     private long _lastEventId;
//
//     private bool _started;
//
//     public Finalizer(FinalizerConfig config, Disruptor<IncomingDisruptorMessage> inputDisruptor, IMessageFactory messageFactory, IClock clock, ILogger<Finalizer> logger)
//     {
//         _inputDisruptor = inputDisruptor;
//         _messageFactory = messageFactory;
//         _clock = clock;
//         _logger = logger;
//         
//         if (!string.IsNullOrEmpty(config.PeriodCutoff))
//         {
//             _timeCutoff = (long)PeriodPattern.Roundtrip.Parse(config.PeriodCutoff).Value.ToDuration().TotalMilliseconds;
//         }
//         
//         _eventIdCutoff = config.EventIdCutoff;
//         
//         _finalizeEveryMessage = string.IsNullOrEmpty(config.PeriodCutoff) && !config.EventIdCutoff.HasValue;
//     }
//     
//     public void Start()
//     {
//         _started = true;
//         _logger.LogInformation($"Finalizer started, last eventId={_lastCutoffEventId}");
//     }
//     
//     public void UpdateLastSentEventId(long lastSentEventId, long timestamp)
//     {
//         _lastCutoffEventId = lastSentEventId;
//         _lastCutoffTime = timestamp;
//     }
//     
//     public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
//     {
//         if (data.Skip) return;
//         
//         if (!_started) throw new InvalidOperationException("Finalizer not started");
//         
//         var finalize = _finalizeEveryMessage;
//         
//         if (data.Value is IEvent e) _lastEventId = e.EventId;
//         // TODO: this doesn't guarantee the delivery of ProjectionEvents
//         
//         if (!finalize && _eventIdCutoff.HasValue)
//         {
//             finalize = _lastEventId - _lastCutoffEventId > _eventIdCutoff.Value;
//         }
//         
//         long now = 0;
//         if (!finalize && _timeCutoff.HasValue)
//         {
//             now = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
//             finalize = now - _lastCutoffTime > _timeCutoff;
//         }
//         
//         if (!finalize) return;
//         
//         _lastCutoffTime = now;
//         _lastCutoffEventId = Math.Max(_lastCutoffEventId, _lastEventId);
//         _logger.LogInformation($"Finalizing, eventId={_lastCutoffEventId}");
//         _inputDisruptor.PublishMessage(new TransportMessage("Finalizer", 
//                 _messageFactory.SerializeAsString(new FinalizeEvt(_lastCutoffEventId, _lastCutoffTime)), now),
//             now,
//             MetricsUtils.GetUnixMicro()
//         );
//     }
//
//     
// }