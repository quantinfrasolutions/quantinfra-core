// using Microsoft.Extensions.Logging;
//
// namespace QuantInfra.Common.ServiceBase.Handlers;
//
// /// <summary>
// /// Checks if the message was already sent downstream
// /// </summary>
// public class DownstreamFilter : Disruptor.IEventHandler<OutgoingDisruptorMessage>
// {
//     private readonly ILogger _logger;
//     
//     private long _lastSentEventId = -1;
//
//     public DownstreamFilter(ILogger<DownstreamFilter> logger)
//     {
//         _logger = logger;
//     }
//     public void Start()
//     {
//         _lastSentEventId = Math.Max(_lastSentEventId, 0);
//         _logger.LogInformation($"Starting DownstreamFilter, lastSentEventId={_lastSentEventId}");
//     }
//
//     public void UpdateLastSentEventId(long lastSentEventId)
//     {
//         _lastSentEventId = lastSentEventId;
//     }
//
//     private bool _startEventLogged = false;
//     public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
//     {
//         if (_lastSentEventId < 0) throw new InvalidOperationException("DownstreamFilter not started");
//         if (data.Value is FinalizeEvt evt)
//         {
//             _lastSentEventId = evt.EventId;
//         }
//
//         if (data.Value is not IEvent e) return;
//         
//         var skip =  e.EventId < _lastSentEventId;
//         if (!skip && _startEventLogged)
//         {
//             _startEventLogged = true;
//             _logger.LogInformation($"First live event: {e.EventId}");
//         }
//         data.Skip = skip;
//     }
// }