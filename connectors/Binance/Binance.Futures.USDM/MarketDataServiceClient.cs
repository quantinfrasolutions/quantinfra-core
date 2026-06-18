// using System.Net.WebSockets;
// using System.Text;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using Binance.Common;
// using Binance.Futures.USDM.Messages.MarketData;
// using Common.MarketData;
// using Common.MarketData.Infrastructure;
// using Common.Messaging;
// using Common.Metrics;
// using Connectors.Common;
// using Disruptor;
// using GenericWebSocketClient;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using Prometheus;
// using QuantInfra.Common.MarketData.Abstractions;
// using QuantInfra.Domain.Events.MarketData;
// using Disruptor.Dsl;
// using QuanInfra.Common.ServiceBase;
//
// namespace Binance.Futures.USDM;
//
// /// <summary>
// /// Binance has internal delays when sending closed bars. For some symbols these delays can be up to 3 seconds (independently on the number of active subscriptions).
// /// Internal processing delays for the connector (~120 active subscriptions):
// ///     * No disruptor (ParserCount == 1): receive_bar_delay average 61.53 ms, processing_time average 36 us. Another test — 200 us
// ///     * ParserCount == 4: receive_bar_delay average 62 ms, processing_time average 135 us
// ///     * Makes no sense to use parallelization
// /// </summary>
// public class MarketDataServiceClient : GenericWebSocketClient.Client,
//                                 IHostedService, 
//                                 IMarketDataClient<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription>,
//                                 IMarketDataClient<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription>,
//                                 IEventHandler<IncomingDisruptorMessage>
// {
//     private readonly MarketDataClientConfig _config;
//     private readonly IBinanceActiveSubscriptionsRepository _repository;
//     private IBinanceOrderBookSubscriptionsRepository _obRepository;
//     private readonly ILogger<MarketDataClient> _logger;
//     private readonly bool _enableTransactionLogging;
//     
//     private readonly IPublisher _publisher;
//     private Dictionary<int, BinanceUsdmMarketDataSubscription> _subscriptions = new();
//     private Dictionary<int, BinanceUsdmOrderBookSubscription> _obSubscriptions = new();
//     private readonly Dictionary<string, int> _streamsMap = new();
//     private readonly Dictionary<string, int> _obContractsMapping = new();
//     private readonly SubscriptionsManager<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription> _subscriptionsManager;
//     private readonly SubscriptionsManager<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription> _obSubscriptionsManager;
//
//     private readonly IClock _clock = SystemClock.Instance;
//     private readonly bool _writePerformanceMetrics;
//     private readonly Histogram? _receiveBarDelay;
//     private readonly Histogram? _receiveClosedBarDelay;
//     private readonly Histogram? _processingTime;
//     private readonly Histogram? _receiveCloseBarCloseDt;
//     
//     private readonly Disruptor<IncomingDisruptorMessage> _disruptor;
//     private readonly bool _useDirtuptor;
//     
//     private readonly JsonSerializerOptions _serializerOptions = new ()
//     {
//         NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
//     };
//
//
//     public MarketDataServiceClient(
//         MarketDataClientConfig config, 
//         IBinanceActiveSubscriptionsRepository repository, 
//         IBinanceOrderBookSubscriptionsRepository obRepository,
//         ILogger<MarketDataClient> logger,
//         IPublisherFactory publisherFactory
//     ) : base(config, logger)
//     {
//         _config = config;
//         _enableTransactionLogging = config.EnableLogging;
//         _repository = repository;
//         _obRepository = obRepository;
//         _logger = logger;
//
//         if (_config.ParsersCount > 1)
//         {
//             _useDirtuptor = true;
//             _disruptor = new(() => new(), 1024);
//
//             var workers = Enumerable.Range(0, config.ParsersCount).Select(_ => new Parser(_streamsMap)).ToArray();
//             _disruptor.HandleEventsWithWorkerPool(workers).Then(this);
//             _disruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<IncomingDisruptorMessage>(logger));
//             _disruptor.Start();
//         }
//
//         _publisher = publisherFactory.GetPublisher("MarketDataServiceInput");
//
//         _subscriptionsManager = new SubscriptionsManager<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription>(
//             (request, reqId) => new BinanceUsdmMarketDataSubscription(reqId, request.StreamId, request.SubscriptionType, request.Symbol),
//             SendSubscribeMessage
//         );
//
//         _obSubscriptionsManager = new(
//             (request, reqId) => new BinanceUsdmOrderBookSubscription(_config.ClientName, reqId, request.ContractId, request.Symbol, request.Frequency),
//             SendOrderBookSubscribeMessage
//         );
//
//         if (config.WritePerformanceMetrics)
//         {
//             _writePerformanceMetrics = true;
//             
//             _receiveBarDelay = Metrics.CreateHistogram(
//                 "receive_bar_delay",
//                 "Difference between Binance timestamp in the bar and reception time in milliseconds", 
//                 new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(10, 10, 10)}
//             );
//             
//             _receiveClosedBarDelay = Metrics.CreateHistogram(
//                 "receive_closed_bar_delay",
//                 "Difference between Binance timestamp and Binance candle close time in milliseconds",
//                 new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(50, 50, 10)}
//             );
//             
//             _receiveCloseBarCloseDt = Metrics.CreateHistogram("receive_closed_bar_delay_close_dt",
//                 "Difference between the close time of the closed bar and the reception time in milliseconds");
//             
//             _processingTime = SharedMetricsDefinition.ProcessingTime;
//         }
//     }
//
//     protected override async Task<Uri> GetUri()
//     {
//         var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync(_config.ClientName);
//         _logger.LogInformation($"{activeSubscriptions.Count} active subscriptions");
//
//         var obSubscriptions = await _obRepository.GetActiveSubscriptionsAsync(_config.ClientName);
//         _logger.LogInformation($"{obSubscriptions.Count} order book subscriptions");
//
//         foreach (var s in activeSubscriptions)
//         {
//             _streamsMap.Add(s.Symbol, s.SubscriptionId);
//         }
//
//         foreach (var s in obSubscriptions)
//         {
//             _obContractsMapping.Add(s.Symbol, s.SubscriptionId);
//         }
//         
//         _subscriptions = activeSubscriptions.ToDictionary(
//             s => s.SubscriptionId, 
//             s => (BinanceUsdmMarketDataSubscription)s
//         );
//         
//         _obSubscriptions = obSubscriptions.ToDictionary(
//             s => s.SubscriptionId, 
//             s => (BinanceUsdmOrderBookSubscription)s
//         );
//         
//         foreach (var s in _subscriptions)
//         {
//             _subscriptionsManager.Subscribe(
//                 s.Value, 
//                 _ => true, 
//                 _ => Task.CompletedTask);
//             _subscriptionsManager.ConfirmSubscription(s.Value.SubscriptionId);
//         }
//
//         foreach (var s in _obSubscriptions)
//         {
//             _obSubscriptionsManager.Subscribe(
//                 s.Value, 
//                 _ => true, 
//                 _ => Task.CompletedTask);
//             _subscriptionsManager.ConfirmSubscription(s.Value.SubscriptionId);
//         }
//
//         var uriBuilder = new UriBuilder(_config.Uri);
//         if (_subscriptions.Count > 0 || _obSubscriptions.Count > 0)
//         {
//             var streams = _subscriptions.Values.Select(s => GetStreamName(s.Symbol, s.SubscriptionType))
//                 .Union(_obSubscriptions.Values.Select(s => GetOBStramName(s.Symbol, s.Frequency)));
//             uriBuilder.Query = $"?streams={string.Join('/', streams)}";
//         }
//
//         return uriBuilder.Uri;
//     }
//
//     protected override Task OnAfterWebSocketConnectedAsync()
//     {
//         _logger.LogInformation("Connected");
//         return Task.CompletedTask;
//         // var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync();
//         // _logger.LogInformation($"{activeSubscriptions.Count} active subscriptions");
//         //
//         // await Task.WhenAll(activeSubscriptions.Select(s => Task.Run(() => Subscribe(s))));
//     }
//
//     protected override void OnStop()
//     {
//         _publisher.Dispose();
//     }
//
//     protected override void ProcessMessage(IngressMessage message)
//     {
//         if (_useDirtuptor)
//         {
//             using var scope = _disruptor.PublishEvent();
//             {
//                 var data = scope.Event();
//                 data.Set(message.Buffer, message.Length, message.ReceivedAt, message.SwReceivedAt);
//             }
//         }
//         else
//         {
//             var msg = new IncomingDisruptorMessage();
//             msg.Set(message.Buffer, message.Length, message.ReceivedAt, message.SwReceivedAt);
//             Parser.OnEvent(_streamsMap, msg);
//             OnEvent(msg, 0, false);
//         }
//     }
//     
//     // protected override void ProcessMessage(JsonDocument document, long receivedAt, long swMicro)
//     // {
//     //     // Thrown exceptions will be handled in the calling method
//     //
//     //     #if PROFILE
//     //     using var processStep = Profiler.Step("ProcessMessage");
//     //     #endif
//     //     
//     //     // Stream message
//     //     if (document.RootElement.TryGetProperty("stream", out var streamProperty))
//     //     {
//     //         string? stream;
//     //         
//     //         #if PROFILE
//     //         using (var step = Profiler.Step("GetStream"))
//     //         {
//     //         #endif
//     //             
//     //         stream = streamProperty.GetString();
//     //         
//     //         #if PROFILE
//     //         }
//     //         #endif
//     //
//     //         BinanceUsdmMarketDataSubscription? subscription;
//     //         
//     //         #if PROFILE
//     //         using (var step = Profiler.Step("GetSubscription"))
//     //         {
//     //         #endif
//     //         
//     //         if (!_streams.TryGetValue(stream!, out subscription))
//     //         {
//     //             _logger.LogWarning($"Received message for unknown stream {stream}");
//     //             return;
//     //         }
//     //         
//     //         #if PROFILE
//     //         }
//     //         #endif
//     //
//     //         JsonElement data;
//     //         string? type;
//     //             
//     //         #if PROFILE
//     //         using (var step = Profiler.Step("GetDataAndType"))
//     //         {
//     //         #endif
//     //             
//     //         data = document.RootElement.GetProperty("data");
//     //         type = data.GetProperty("e").GetString();
//     //         
//     //         #if PROFILE
//     //         }
//     //         #endif
//     //
//     //         switch (type)
//     //         {
//     //             case "kline":
//     //                 KlineData? message;
//     //                     
//     //                 #if PROFILE
//     //                 using (var step = Profiler.Step("DeserializeKline"))
//     //                 {
//     //                 #endif
//     //                     
//     //                 message = data.Deserialize<KlineData>(_serializerOptions);
//     //                 
//     //                 #if PROFILE
//     //                 }
//     //                 #endif
//     //                 
//     //                 ProcessKLine(subscription, message, receivedAt, swMicro);
//     //                 break;
//     //             default:
//     //                 _logger.LogWarning($"Unsupported eventType {type}");
//     //                 return;
//     //         }
//     //     }
//     //     // Subscribe/unsubscribe message
//     //     else if (document.RootElement.TryGetProperty("id", out var idProperty))
//     //     {
//     //         var msg = document.Deserialize<ServiceMessage>(_serializerOptions);
//     //         ProcessServiceMessage(msg);
//     //     }
//     // }
//     
//     public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
//     {
//         if (data.ConfirmedSubscriptionId.HasValue) Task.Run(() => _subscriptionsManager.ConfirmSubscription(data.ConfirmedSubscriptionId.Value));
//         else if (data.Kline1m.HasValue)
//         {
//             ProcessKLine(data.Kline1m.Value, data.ReceivedAt, data.SwReceivedAt);
//         }
//     }
//
//     private void ProcessServiceMessage(ServiceMessage? msg)
//     {
//         if (msg == null) throw new InvalidOperationException("Msg is null");
//         _subscriptionsManager.ConfirmSubscription(msg.RequestId);
//     }
//
//     private void ProcessKLine(Kline1m message, long receivedAt, long swReceivedAt)
//     {
//         #if PROFILE
//         using var processStep = Profiler.Step("ProcessKLine");
//         #endif
//
//         ExchangeBar bar;
//         
//         #if PROFILE
//         using (var step = Profiler.Step("ToExchangeBar"))
//         {
//         #endif
//         
//         var subscription = _subscriptions[message.SubscriptionId];
//         
//         var streamId = subscription.StreamId;
//         
//         bar = message.ToExchangeBar(subscription.StreamId ?? 0, _config.DatasourceId, _config.TradingSessionId);
//         
//         #if PROFILE
//         }
//         #endif
//         
//         subscription.LastBar = bar;
//         
//         if (_writePerformanceMetrics)
//         {
//             _receiveBarDelay!.Observe(receivedAt - message.Timestamp);
//             // _processingDelay!.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);
//         }
//
//         if (message.IsClosed != true)
//         {
//             if (streamId.HasValue)
//             {
//                 _publisher.PublishUnwrappedObjectWithReceiptionSwMicro(new StreamLastPriceUpdatedEvt(streamId.Value, message.Close, 
//                     Instant.FromUnixTimeMilliseconds(message.Timestamp)), swReceivedAt);
//                 
//                 if (_writePerformanceMetrics)
//                 {
//                     _processingTime!.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);
//                 }
//             }
//
//             return;
//         }
//         
//         if (_writePerformanceMetrics)
//         {
//             _receiveClosedBarDelay!.Observe(message.Timestamp - message.CloseTimeMs);
//             _receiveCloseBarCloseDt!.Observe(receivedAt - bar.CloseDt.ToUnixTimeMilliseconds());
//         }
//
//         if (!streamId.HasValue)
//         {
//             return;
//         }
//         
//         #if PROFILE
//         using (var step = Profiler.Step("Log"))
//         {
//         #endif
//             
//         if (_enableTransactionLogging) _logger.LogDebug($"Sending bar {bar}");
//         
//         #if PROFILE
//         }
//         #endif
//         
//         #if PROFILE
//         using (var step = Profiler.Step("PublishBar"))
//         {
//         #endif
//         
//         _publisher.PublishUnwrappedObjectWithReceiptionSwMicro(new ExchangeBarReceivedEvt(bar), swReceivedAt);
//         
//         #if PROFILE
//         }
//         #endif
//
//         if (_writePerformanceMetrics)
//         {
//             _processingTime!.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);
//         }
//     }
//
//     private string GetStreamName(string symbol, SubscriptionDataType type) =>
//         $"{symbol.ToLower()}@{type.GetBinanceSubscriptionType()}";
//     
//     private string GetOBStramName(string symbol, int frequency)
//     {
//         var sb = new StringBuilder();
//         sb.Append($"{symbol.ToLower()}@depth");
//         if (frequency != 250) sb.Append($"{frequency}");
//         return sb.ToString();
//     }
//
//     public IReadOnlyCollection<BinanceUsdmMarketDataSubscription> GetActiveSubscriptions() => 
//         _subscriptionsManager.Subscriptions.Values.ToList();
//     
//     IReadOnlyCollection<BinanceUsdmOrderBookSubscription> IMarketDataClient<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription>.GetActiveSubscriptions() =>
//         _obSubscriptionsManager.Subscriptions.Values.ToList();
//     
//     public async Task Subscribe(BinanceUsdmOrderBookSubscriptionRequest request)
//     {
//         try
//         {
//             var subscription = await _obSubscriptionsManager.Subscribe(request);
//             _obContractsMapping.Add(request.Symbol, subscription.SubscriptionId);
//             _obSubscriptions.Add(subscription.SubscriptionId, subscription);
//             await _obRepository.AddSubscriptionAsync(subscription);
//             _logger.LogInformation($"Successfully subscribed: {subscription}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Failed to create subscription");
//             throw;
//         }
//     }
//
//     public async Task Subscribe(BinanceUsdmMarketDataSubscriptionRequest request)
//     {
//         try
//         {
//             var subscription = await _subscriptionsManager.Subscribe(request);
//             _streamsMap.Add(request.Symbol, subscription.SubscriptionId);
//             _subscriptions.Add(subscription.SubscriptionId, subscription);
//             await _repository.AddSubscriptionAsync(subscription);
//             _logger.LogInformation($"Successfully subscribed: {subscription}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Failed to create subscription");
//             throw;
//         }
//     }
//
//     private Task SendSubscribeMessage(BinanceUsdmMarketDataSubscription subscription)
//     {
//         if (WebSocket.State != WebSocketState.Open) throw new Exception("Gateway is not connected");
//         
//         _logger.LogInformation($"Subscribe: {subscription}");
//         var streamName = GetStreamName(subscription.Symbol, subscription.SubscriptionType);
//         var msg = SubscribeMsg.CreateForSingleStream(streamName, subscription.SubscriptionId);
//         return SendMessageAsync(msg);
//     }
//     
//     private Task SendOrderBookSubscribeMessage(BinanceUsdmOrderBookSubscription subscription)
//     {
//         if (WebSocket.State != WebSocketState.Open) throw new Exception("Gateway is not connected");
//         
//         _logger.LogInformation($"Subscribe: {subscription}");
//         var streamName = GetOBStramName(subscription.Symbol, subscription.Frequency);
//         var msg = SubscribeMsg.CreateForSingleStream(streamName, subscription.SubscriptionId);
//         return SendMessageAsync(msg);
//     }
//
//     public Task Unsubscribe(int requestId)
//     {
//         throw new NotImplementedException();
//     }
//     
// }