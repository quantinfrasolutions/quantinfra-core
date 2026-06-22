using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Binance.Common;
using Binance.Futures.USDM;
using Common.Metrics;
using Disruptor.Dsl;
using GenericWebSocketClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Binance.Futures.USDM.MarketData;
using QuantInfra.Common.MarketData.Infrastructure;
using QuantInfra.Common.MarketData.OrderBooks;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;
using QuantInfra.Connectors.Common;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Services.MarketData;
using OrderBookSnapshot = QuantInfra.Binance.Futures.USDM.MarketData.OrderBookSnapshot;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

/// <summary>
/// Binance has internal delays when sending closed bars. For some symbols these delays can be up to 3 seconds (independently on the number of active subscriptions).
/// Internal processing delays for the connector (~120 active subscriptions):
///     * No disruptor (ParserCount == 1): receive_bar_delay average 61.53 ms, processing_time average 36 us. Another test — 200 us
///     * ParserCount == 4: receive_bar_delay average 62 ms, processing_time average 135 us
///     * Makes no sense to use parallelization
/// </summary>
public class MarketDataClient : GenericWebSocketClient.Client,
                                IHostedService, 
                                IMarketDataClient<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription>,
                                IMarketDataClient<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription>, Disruptor.IEventHandler<IncomingDisruptorMessage>,
                                IMarketDataSnapshotsProvider
{
    private readonly MarketDataClientConfig _config;
    private readonly IBinanceActiveSubscriptionsRepository _repository;
    private readonly IBinanceOrderBookSubscriptionsRepository _obRepository;
    private readonly ILogger<MarketDataClient> _logger;
    private readonly bool _enableTransactionLogging;
    
    private Dictionary<int, BinanceUsdmMarketDataSubscription> _subscriptions = new();
    private Dictionary<int, BinanceUsdmOrderBookSubscription> _obSubscriptions = new();
    private readonly Dictionary<string, int> _streamsMap = new();
    private readonly Dictionary<string, int> _obContractsMapping = new();
    private readonly SubscriptionsManager<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription> _subscriptionsManager;
    private readonly SubscriptionsManager<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription> _obSubscriptionsManager;

    private readonly IClock _clock = SystemClock.Instance;
    private readonly bool _writePerformanceMetrics;
    private readonly Histogram? _receiveBarDelay;
    private readonly Histogram? _receiveClosedBarDelay;
    private readonly Histogram? _processingTime;
    private readonly Histogram? _receiveCloseBarCloseDt;
    private readonly Histogram? _receiveOrderBookUpdateDelay;
    
    private readonly Disruptor<IncomingDisruptorMessage> _disruptor;
    
    private readonly Dictionary<int, int> _contractsBySubscription = new();
    
    
    private readonly JsonSerializerOptions _serializerOptions = new ()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private readonly BinanceMarketDataClient _restClient;
    private readonly Bpl _bpl;


    public MarketDataClient(
        MarketDataClientConfig config,
        DisruptorConfig disruptorConfig,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        IBinanceActiveSubscriptionsRepository repository, 
        IBinanceOrderBookSubscriptionsRepository obRepository,
        Bpl bpl,
        ILogger<MarketDataClient> logger
    ) : base(config, logger)
    {
        _config = config;
        _enableTransactionLogging = config.EnableLogging;
        _repository = repository;
        _obRepository = obRepository;
        _bpl = bpl;
        _logger = logger;
        
        _disruptor = new(() => new(), disruptorConfig.InputDisruptorRingBufferSize,
            TaskScheduler.Default);
        _disruptor.HandleEventsWith(this);
        
        // if (_config.ParsersCount > 1)
        // {
        //     _useDirtuptor = true;
        //     _disruptor = new(() => new(), 1024);
        //
        //     var workers = Enumerable.Range(0, config.ParsersCount).Select(_ => new Parser(_streamsMap)).ToArray();
        //     _disruptor.HandleEventsWithWorkerPool(workers).Then(this);
        //     _disruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<IncomingDisruptorMessage>(logger));
        //     _disruptor.Start();
        // }

        _subscriptionsManager = new(
            (request, reqId) => new BinanceUsdmMarketDataSubscription(reqId, request.StreamId, request.SubscriptionType, request.Symbol),
            SendSubscribeMessage
        );
        
        _obSubscriptionsManager = new(
            (request, reqId) => new BinanceUsdmOrderBookSubscription(_config.ClientName, reqId, request.ContractId, request.Symbol, request.Frequency, request.Levels),
            SendOrderBookSubscribeMessage
        );


        _restClient = new BinanceMarketDataClient(new HttpClient()
        {
            BaseAddress = new Uri(_config.RestUri)
        });

        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            
            _receiveBarDelay = Metrics.CreateHistogram(
                config.Monolith ? $"{config.ClientName}_receive_bar_delay" : "receive_bar_delay",
                "Difference between Binance timestamp in the bar and reception time in milliseconds", 
                new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(config.ReceiveBarDelayParams[0],
                    config.ReceiveBarDelayParams[1], config.ReceiveBarDelayParams[2]) }
            );

            _receiveOrderBookUpdateDelay = Metrics.CreateHistogram(
                config.Monolith ? $"{config.ClientName}_receive_ob_update_delay" : "receive_ob_update_delay",
                "Difference between Binance timestamp in the order book update and reception time, ms",
                new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(config.ReceiveObDelayParams[0],
                    config.ReceiveObDelayParams[1], config.ReceiveObDelayParams[2])}
            );
            
            _receiveClosedBarDelay = Metrics.CreateHistogram(
                config.Monolith ? $"{config.ClientName}_receive_closed_bar_delay": "receive_closed_bar_delay",
                "Difference between Binance timestamp and Binance candle close time in milliseconds",
                new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(config.ReceiveClosedBarDelayParams[0],
                    config.ReceiveClosedBarDelayParams[1], config.ReceiveClosedBarDelayParams[2])}
            );
            
            _receiveCloseBarCloseDt = Metrics.CreateHistogram(
                config.Monolith ? $"{config.ClientName}_receive_closed_bar_delay_close_dt" : "receive_closed_bar_delay_close_dt",
                "Difference between the close time of the closed bar and the reception time in milliseconds");
            
            _processingTime = SharedMetricsDefinition.GetProcessingTime(config.ClientName, config.Monolith);
        }
    }

    protected override Task OnBeforeStartAsync()
    {
        _disruptor.Start();
        return _bpl.StartAsync(CancellationToken.None);
    }

    protected override async Task<Uri> GetUri()
    {
        var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync(_config.ClientName);
        _logger.LogInformation($"{activeSubscriptions.Count} active subscriptions");

        var obSubscriptions = await _obRepository.GetActiveSubscriptionsAsync(_config.ClientName);
        _logger.LogInformation($"{obSubscriptions.Count} order book subscriptions");

        foreach (var s in obSubscriptions)
        {
            _contractsBySubscription.Add(s.SubscriptionId, s.ContractId);
            _obContractsMapping.Add(s.Symbol, s.SubscriptionId);
        }
        
        _subscriptions = activeSubscriptions.ToDictionary(
            s => s.SubscriptionId, 
            s => (BinanceUsdmMarketDataSubscription)s
        );
        
        _obSubscriptions = obSubscriptions.ToDictionary(
            s => s.SubscriptionId, 
            s => (BinanceUsdmOrderBookSubscription)s
        );
        
        foreach (var s in _subscriptions)
        {
            _subscriptionsManager.Subscribe(
                s.Value, 
                _ => true, 
                _ => Task.CompletedTask);
            _subscriptionsManager.ConfirmSubscription(s.Value.SubscriptionId);
            
            _streamsMap.Add(s.Value.Symbol, s.Key);
        }

        foreach (var s in _obSubscriptions)
        {
            _obSubscriptionsManager.Subscribe(
                s.Value, 
                _ => true, 
                _ => Task.CompletedTask);
            _obSubscriptionsManager.ConfirmSubscription(s.Value.SubscriptionId);
        }

        var uriBuilder = new UriBuilder(_config.Uri);
        if (_subscriptions.Count > 0 || _obSubscriptions.Count > 0)
        {
            var streams = _subscriptions.Values.Select(s => GetStreamName(s.Symbol, s.SubscriptionType))
                .Union(_obSubscriptions.Values.Select(s => GetOBStramName(s.Symbol, s.Frequency)));
            uriBuilder.Query = $"?streams={string.Join('/', streams)}";
        }

        return uriBuilder.Uri;
    }

    protected override async Task OnAfterWebSocketConnectedAsync()
    {
        _logger.LogInformation("Connected");

        await Task.WhenAll(_obSubscriptions.Select(s => Task.Run(async () =>
        {
            var res = await _restClient.GetOrderBookSnapshotAsync(s.Value.Symbol, s.Value.Levels);

            if (res is null) throw new Exception("Received null order book snapshot from Binance");
            using var scope = _disruptor.PublishEvent();
            var evt = scope.Event();
            res.ContractId = s.Value.ContractId;
            evt.SetOrderBookSnapshot(res);
            evt.SwReceivedAt = MetricsUtils.GetUnixMicro();
            evt.ReceivedAt = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        })));
        
        _logger.LogInformation("Orderbook snapshots retrieved");
    }

    protected override void OnStop() { }

    protected override void ProcessMessage(IngressMessage message)
    {
        var kind = BinanceMessageRouter.Classify(message.Buffer, out var svc);
        switch (kind)
        {
            case BinanceMsgKind.ServiceAck:
                // TODO: confirm subscription
                return;
            case BinanceMsgKind.MarketData:
                var json = new ReadOnlySpan<byte>(message.Buffer, 0, message.Length);
                if (OrderBookDiffParser.TryParseDepthDiff(json, _obContractsMapping, out var depthDiff))
                {
                    using var scope = _disruptor.PublishEvent();
                    var evt = scope.Event();
                    evt.SetOrderBookUpdate(depthDiff);
                    return;
                }

                if (Kline1mParser.TryParseKline1m(json, _streamsMap, out var kline))
                {
                    using var scope = _disruptor.PublishEvent();
                    var evt = scope.Event();
                    evt.SetKline1m(kline);
                    return;
                }

                return;
        }
    }
    
    public void RequestOrderBookSnapshot(GetOrderBookSnapshot request)
    {
        using var scope = _disruptor.PublishEvent();
        var evt = scope.Event();
        evt.SetGetOrderBookSnapshot(request);
    }
    
    
    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        var swStartProcessing = MetricsUtils.GetUnixMicro();
        
        switch (data.Type)
        {
            case IncomingMessageType.Kline:
                ProcessKLine(data.Kline1m!.Value, data.ReceivedAt, data.SwReceivedAt, swStartProcessing);
                break;
            
            case IncomingMessageType.OrderBookSnapshot:
                ProcessOrderBookSnapshot(data.OrderBookSnapshot!, data.ReceivedAt, data.SwReceivedAt, swStartProcessing);
                break;
            
            case IncomingMessageType.OrderBookUpdate:
                ProcessOrderBookDiff(data.OrderbookDiff!.Value, data.ReceivedAt, data.SwReceivedAt, swStartProcessing);
                break;
            
            case IncomingMessageType.GetOrderBookSnapshot:
                _bpl.SendOrderBookSnapshot(data.GetOrderBookSnapshot!);
                break;
            
            default:
                throw new NotSupportedException();
        }
    }

    private void ProcessServiceMessage(ServiceMessage? msg)
    {
        if (msg == null) throw new InvalidOperationException("Msg is null");
        _subscriptionsManager.ConfirmSubscription(msg.RequestId);
    }
    
    private readonly Dictionary<int, OrderBook> _orderbooks = new();
    private readonly Dictionary<int, List<OrderBookDiff>> _updatesBuffer = new();
    private void ProcessOrderBookDiff(OrderBookDiff diff, long receivedAt, long swReceivedAt, long swStartProcessing)
    {
        var subscriptionId = diff.SubscriptionId;
        
        if (!_contractsBySubscription.TryGetValue(subscriptionId, out var contractId)) return;
        
        if (!_orderbooks.TryGetValue(contractId, out var ob))
        {
            if (!_updatesBuffer.ContainsKey(contractId))
            {
                _updatesBuffer[contractId] = new(100); // TODO: configurable count
            }
            _updatesBuffer[contractId].Add(diff);
            return;
        }

        var exchangeTs = Instant.FromUnixTimeMilliseconds(diff.EventTimeMs);
        
        _bpl.OnOrderbookUpdated(
            contractId, 
            new ArraySegment<OrderBookLevelUpdate>(diff.Bids, 0, diff.BidCount).ToDictionary(i => i.Price, i => i.Quantity), 
            new ArraySegment<OrderBookLevelUpdate>(diff.Asks, 0, diff.AskCount).ToDictionary(i => i.Price, i => i.Quantity),
            exchangeTs, swReceivedAt, swStartProcessing
        );

        _obSubscriptions[subscriptionId].LastUpdate = exchangeTs;
        
        _receiveOrderBookUpdateDelay?.Observe(receivedAt - diff.EventTimeMs);
    }

    private void ProcessOrderBookSnapshot(OrderBookSnapshot snapshot, long receivedAt, long swReceivedAt,
        long swStartProcessing)
    {
        var contractId = snapshot.ContractId;
        if (!_orderbooks.TryGetValue(contractId, out var orderbook))
        {
            var ob = new OrderBook(snapshot.ContractId, Math.Max(snapshot.Bids.Count, snapshot.Asks.Count));
            _orderbooks[contractId] = ob;

            var snapshotTs = Instant.FromUnixTimeMilliseconds(snapshot.T);
            
            foreach (var ask in snapshot.Asks)
            {
                ob.Update(BookSide.Ask, ask[0], ask[1], snapshotTs);
            }
            foreach (var bid in snapshot.Bids)
            {
                ob.Update(BookSide.Bid, bid[0], bid[1], snapshotTs);
            }
			
            if (_updatesBuffer.Remove(contractId, out var buffer))
            {
                foreach (var update in buffer)
                {
                    var updateTs = Instant.FromUnixTimeMilliseconds(update.EventTimeMs);
                    if (update.FinalUpdateId < snapshot.LastUpdateId) continue;
                    foreach (var ask in update.Asks) ob.Update(BookSide.Ask, ask.Price, ask.Quantity, updateTs);
                    foreach (var bid in update.Bids) ob.Update(BookSide.Bid, bid.Price, bid.Quantity, updateTs);
                }
            }
            
            _bpl.OnOrderBookSnapshot(contractId, ob.GetSnapshot(), swReceivedAt, swStartProcessing);
        }
    }

    private void ProcessKLine(Kline1m message, long receivedAt, long swReceivedAt, long swStartedProcessing)
    {
        ExchangeBar bar;
        
        var subscription = _subscriptions[message.SubscriptionId];
        var streamId = subscription.StreamId;
        bar = message.ToExchangeBar(subscription.StreamId ?? 0, _config.DatasourceId, _config.TradingSessionId);
        
        subscription.LastBar = bar;
        
        _receiveBarDelay?.Observe(receivedAt - message.Timestamp);
        // _processingDelay?.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);

        if (message.IsClosed != true)
        {
            if (streamId.HasValue)
            {
                _bpl.OnStreamLastPriceUpdatedEvt(
                    new StreamLastPriceUpdatedEvt(streamId.Value, message.Close, Instant.FromUnixTimeMilliseconds(message.Timestamp)),
                    swReceivedAt, swStartedProcessing
                );
                _processingTime?.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);
            }

            return;
        }
        
        if (_writePerformanceMetrics)
        {
            _receiveClosedBarDelay!.Observe(message.Timestamp - message.CloseTimeMs);
            _receiveCloseBarCloseDt!.Observe(receivedAt - bar.CloseDt.ToUnixTimeMilliseconds());
        }

        if (!streamId.HasValue) return;
        
        _bpl.OnExchangeBarEvt(new ExchangeBarReceivedEvt(bar), swReceivedAt, swStartedProcessing);
        _processingTime?.Observe(MetricsUtils.GetUnixMicro() - swReceivedAt);
    }

    private string GetStreamName(string symbol, SubscriptionType type) =>
        $"{symbol.ToLower()}@{type.GetBinanceSubscriptionType()}";
    
    private string GetOBStramName(string symbol, int frequency)
    {
        var sb = new StringBuilder();
        sb.Append($"{symbol.ToLower()}@depth");
        if (frequency != 250) sb.Append($"{frequency}");
        return sb.ToString();
    }

    public IReadOnlyCollection<BinanceUsdmMarketDataSubscription> GetActiveSubscriptions() => _subscriptionsManager.Subscriptions.Values.ToList();
    public Task Subscribe(BinanceUsdmOrderBookSubscriptionRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task Subscribe(BinanceUsdmMarketDataSubscriptionRequest request)
    {
        try
        {
            var subscription = await _subscriptionsManager.Subscribe(request);
            _streamsMap.Add(request.Symbol, subscription.SubscriptionId);
            _subscriptions.Add(subscription.SubscriptionId, subscription);
            await _repository.AddSubscriptionAsync(subscription);
            _logger.LogInformation($"Successfully subscribed: {subscription}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create subscription");
            throw;
        }
    }

    private Task SendSubscribeMessage(BinanceUsdmMarketDataSubscription subscription)
    {
        if (WebSocket.State != WebSocketState.Open) throw new Exception("Gateway is not connected");
        
        _logger.LogInformation($"Subscribe: {subscription}");
        var streamName = GetStreamName(subscription.Symbol, subscription.SubscriptionType);
        var msg = SubscribeMsg.CreateForSingleStream(streamName, subscription.SubscriptionId);
        return SendMessageAsync(msg);
    }
    
    private Task SendOrderBookSubscribeMessage(BinanceUsdmOrderBookSubscription subscription)
    {
        if (WebSocket.State != WebSocketState.Open) throw new Exception("Gateway is not connected");
        
        _logger.LogInformation($"Subscribe: {subscription}");
        throw new NotImplementedException();
        // var streamName = GetStreamName(subscription.Symbol, subscription.SubscriptionType);
        // var msg = SubscribeMsg.CreateForSingleStream(streamName, subscription.SubscriptionId);
        // return SendMessageAsync(msg);
    }

    IReadOnlyCollection<BinanceUsdmOrderBookSubscription> QuantInfra.Common.MarketData.Infrastructure.IMarketDataClient<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription>.GetActiveSubscriptions()
    {
        throw new NotImplementedException();
    }

    public Task Unsubscribe(int requestId)
    {
        throw new NotImplementedException();
    }
}