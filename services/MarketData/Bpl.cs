using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Metrics;
using Common.StaticData.Abstractions;
using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.MarketData;
using QuantInfra.Common.MarketData.OrderBooks;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Services.MarketData.Jobs;
using Quartz;
using Contract = QuantInfra.Sdk.StaticData.Contract;
using Stream = QuantInfra.Sdk.StaticData.Stream;

[assembly:InternalsVisibleTo("Tests.v6.E2E")]

namespace QuantInfra.Services.MarketData;

public class Bpl : Disruptor.IEventHandler<IncomingDisruptorMessage>, IHostedService
{
	private readonly ILogger _logger;
	private readonly Config _config;
	private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
	private readonly IMarketDataServiceStreamsRepository _streamsRepository;
	private readonly IClock _clock;
	private readonly IReceiverStateProvider _receiverStateProvider;

	private readonly ISchedulerFactory _schedulerFactory;
	private readonly bool _writePerformanceMetrics;
	
	private readonly Histogram? _receiveBarHop;
	private readonly Histogram? _processingDelay;
	private readonly Histogram? _processingTime;
	
	private Dictionary<int, Contract> _contractsByStream;
	private Calculator _calculator;
	private TradingSessionWatcher<int> _tsw;

	public Bpl(
		Config config,
		Disruptor<OutgoingDisruptorMessage> outputDisruptor,
		IMarketDataServiceStreamsRepository streamsRepository,
		ILoggerFactory loggerFactory,
		ISchedulerFactory schedulerFactory, 
		IClock clock,
		IReceiverStateProvider receiverStateProvider
	)
	{
		_config = config;
		_outputDisruptor = outputDisruptor;
		_streamsRepository = streamsRepository;
		_schedulerFactory = schedulerFactory;
		_clock = clock;
		_receiverStateProvider = receiverStateProvider;
		_schedulerFactory = schedulerFactory;
		_logger = loggerFactory.CreateLogger("MarketDataService");
			
		if (config.WritePerformanceMetrics)
		{
			_writePerformanceMetrics = true;
			
			_receiveBarHop = SharedMetricsDefinition.ReceiveBarHop;
			_processingDelay = SharedMetricsDefinition.ProcessingDelay;
			_processingTime  = SharedMetricsDefinition.ProcessingTime;
		}
	}
	
	
    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;
        if (data.IsReplay) return;

        var receivedAt = data.ReceivedAt;
        var swStartProcessing = MetricsUtils.GetUnixMicro();
        
        if (data.TransportMessage is not null)
        {
	        _receiverStateProvider.UpdateState(data.TransportMessage.SenderCompId, data.TransportMessage.SessionId, data.TransportMessage.SequenceNumber);
	        if (_writePerformanceMetrics)
	        {
		        _receiveBarHop!.Observe(receivedAt - data.TransportMessage.SendingTimestamp);
	        }
        }
        
        long swReceivedAt = 0;
        if (_writePerformanceMetrics)
        {
	        swReceivedAt = data.SwReceivedAt;
	        _processingDelay!.Observe(swStartProcessing - swReceivedAt);
        }

        if (data.ParsedMessage == null) return;
        
        switch (data.ParsedMessage)
        {
	        case ExchangeTradeReceivedEvt @event:
		        OnExchangeTradeEvt(@event, swReceivedAt, swStartProcessing);
		        break;
	        case ExchangeBarReceivedEvt @event:
		        OnExchangeBarEvt(@event, swReceivedAt, swStartProcessing);
		        break;
	        case StreamLastPriceUpdatedEvt @event:
		        OnStreamLastPriceUpdatedEvt(@event, swReceivedAt, swStartProcessing);
		        break;
	        case AggregatedOrderbookUpdateEvt evt:
		        OnOrderbookUpdated(evt.ContractId, evt.UpdatedBids, evt.UpdatedAsks, evt.ExchangeTs, swReceivedAt, swStartProcessing);
		        break;
	        case OrderBookSnapshotReceivedEvt @event:
		        OnOrderBookSnapshot(@event.ContractId, @event.Snapshot, swReceivedAt, swStartProcessing);
		        break;
	        case GetOrderBookSnapshot getSnapshot:
		        SendOrderBookSnapshot(getSnapshot);
		        break;
	        // case StreamEnabledChangedEvt @event:
		       //  OnStreamEnabledChangedEvt(@event);
		       //  break;
        }
    }

    private void OnExchangeTradeEvt(ExchangeTradeReceivedEvt evt, long swReceivedAt, long swStartedProcessing)
	{
		throw new NotImplementedException();
		// (var time, var volume) = _calculator.AppendTrade(evt.Trade);
  //
		// if (time != null)
		// {
		// 	time.TradingSessionId = _tsw.ProcessUpdateAndGetCurrentSessionId(time.StreamId, time.OpenDt)?.Id;
		// 	_outgoingDisruptor
		// 	_publisher.PublishUnwrappedObject(new BAUClosedEvent(time, _clock.GetCurrentInstant()));
		// 	if (_writePerformanceMetrics)
		// 	{
		// 		_sendBarDelay!.Observe((_clock.GetCurrentInstant() - evt.Trade.Dt).TotalMilliseconds);
		// 	}
		// 	_persister.AppendBAU(time, BarAggregationType.Time);
		// }
  //
		// if (volume != null)
		// {
		// 	_publisher.PublishUnwrappedObject(new BAUClosedEvent(volume, _clock.GetCurrentInstant()));
		// 	_persister.AppendBAU(volume, BarAggregationType.Volume);
  //       }
		//
		// if (_writePerformanceMetrics)
		// {
		// 	_receiveBarDelay!.Observe((receiveTime - evt.Trade.Dt).TotalMilliseconds);
		// }
    }

    public void SendOrderBookSnapshot(GetOrderBookSnapshot request)
    {
	    if (_orderbooks.TryGetValue(request.ContractId, out var ob))
		    _outputDisruptor.PublishMessage(new AsyncQueryResponse<GetOrderBookSnapshot, OrderBookSnapshot?>(request.RequestId, ob.GetSnapshot(), request.UseMulticast));
	    else
		    _outputDisruptor.PublishMessage(new AsyncQueryResponse<GetOrderBookSnapshot, OrderBookSnapshot?>(request.RequestId, null, request.UseMulticast));
    }

	public void OnExchangeBarEvt(ExchangeBarReceivedEvt evt, long swReceivedAt, long swStartedProcessing)
	{
		// Calculator may return 2 closed bars in case there is a gap and the received bar closes the aggregation
        var (time, volume) = _calculator.AppendBar(evt.Bar);

        int? contractId = GetContractIdByStreamId(evt.Bar.StreamId, out var contract)
	        ? contract.ContractId
	        : null;

        int? tradingSessionId = evt.Bar.TradingSessionId;

        if (!tradingSessionId.HasValue && contractId.HasValue)
        {
	        tradingSessionId = _tsw.ProcessUpdateAndGetCurrentSessionId(contractId.Value, evt.Bar.CloseDt)?.TradingSessionId;
        }
        
        if (time != null)
        {
			for (var i = 0; i < time.Length; i++)
			{
				var t = time[i];

				var bar = new ExchangeBar(t)
                {
	                ContractId = contractId,
	                TradingSessionId = tradingSessionId,
                };
                
                var now = _clock.GetCurrentInstant();
                _outputDisruptor.PublishMessage(new Candle1MClosedEvt(bar, now), MetricsUtils.GetUnixMicro(), swStartedProcessing);
			}
        }
        
        if (contractId.HasValue)
        {
	        _outputDisruptor.PublishMessage(new ContractLastPriceUpdatedEvt(contract.ContractId, contract.NormalizePrice(evt.Bar.Close), tradingSessionId, 
		        evt.Bar.CloseDt, _clock.GetCurrentInstant()), MetricsUtils.GetUnixMicro(),swStartedProcessing);
        }

   //      if (volume != null)
   //      {
			// foreach (var v in volume)
			// {
			// 	_publisher.PublishUnwrappedObject(new BAUClosedEvent(v, _clock.GetCurrentInstant()));
			// 	_persister.AppendBAU(v, BarAggregationType.Volume);
			// }
   //      }
        
		_processingTime?.Observe(MetricsUtils.GetUnixMicro() - swStartedProcessing);
    }

	private Dictionary<int, Instant> _lastSentContractUpdates = new(); // stream_id => last sent time
	public void OnStreamLastPriceUpdatedEvt(StreamLastPriceUpdatedEvt evt, long swReceivedAt, long swStartedProcessing)
	{
		var streamId = evt.StreamId;
		var dt = evt.ReferenceDt;
		if (_lastSentContractUpdates.TryGetValue(streamId, out var lastSent) && (dt - lastSent) < _config.ContractPriceUpdateInterval) return;
		
		if (GetContractIdByStreamId(streamId, out var contract))
		{
			var tsId = _tsw.ProcessUpdateAndGetCurrentSessionId(evt.StreamId, evt.ReferenceDt)?.TradingSessionId;
			_outputDisruptor.PublishMessage(new ContractLastPriceUpdatedEvt(contract.ContractId, contract.NormalizePrice(evt.Price), 
				tsId, dt, _clock.GetCurrentInstant()), MetricsUtils.GetUnixMicro(), swStartedProcessing);
			_lastSentContractUpdates[streamId] = dt;
		}
	}
	
	private readonly Dictionary<int, OrderBook> _orderbooks = new();
	public void OnOrderBookSnapshot(int contractId, OrderBookSnapshot snapshot, long swReceivedAt, long swStartProcessing)
	{
		var ob = new OrderBook(snapshot);
		
		_orderbooks[contractId] = ob;
		
		_outputDisruptor.PublishMessage(
		    new AsyncQueryResponse<GetOrderBookSnapshot, OrderBookSnapshot>(
		        Guid.NewGuid(), ob.GetSnapshot(), true),
		    MetricsUtils.GetUnixMicro(),
		    swReceivedAt
		);
		
		var bestBid = ob.Bids.GetBest();
		var bestAsk = ob.Asks.GetBest();
		_outputDisruptor.PublishMessage(new BestBidAskUpdatedEvt(contractId, bestBid, bestAsk, ob.LastUpdate), 
		    MetricsUtils.GetUnixMicro(), swReceivedAt);
		
		_processingTime?.Observe(MetricsUtils.GetUnixMicro() - swStartProcessing);
	}
	
	public void OnOrderbookUpdated(int contractId,
		IReadOnlyDictionary<decimal, decimal> updatedBids, IReadOnlyDictionary<decimal, decimal> updatedAsks, 
		Instant exchangeTs, long swReceivedAt, long swStartProcessing
	)
	{
		if (!_orderbooks.TryGetValue(contractId, out var ob)) return;
		
		var evt = new AggregatedOrderbookUpdateEvt(
			contractId,
			updatedBids, updatedAsks,
			exchangeTs,
			_clock.GetCurrentInstant()
		);
		_outputDisruptor.PublishMessage(evt, MetricsUtils.GetUnixMicro(), swReceivedAt);

		var bestBid = ob.Bids.GetBest();
		var bestAsk = ob.Asks.GetBest();

		foreach (var ask in evt.UpdatedAsks) ob.Update(BookSide.Ask, ask.Key, ask.Value, evt.ExchangeTs);
		foreach (var bid in evt.UpdatedBids) ob.Update(BookSide.Bid, bid.Key, bid.Value, evt.ExchangeTs);
		
		var bestBidAfter = ob.Bids.GetBest();
		var bestAskAfter = ob.Asks.GetBest();
		if (!Nullable.Equals(bestBidAfter, bestBid) || !Nullable.Equals(bestAskAfter, bestAsk))
		{
			_outputDisruptor.PublishMessage(new BestBidAskUpdatedEvt(contractId, bestBidAfter, bestAskAfter, evt.Timestamp), 
				MetricsUtils.GetUnixMicro(), swReceivedAt);
		}
		
		_processingTime?.Observe(MetricsUtils.GetUnixMicro() - swStartProcessing);
	}

	private bool GetContractIdByStreamId(int streamId, out Contract contract) => _contractsByStream.TryGetValue(streamId, out contract);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
	    var streams = await _streamsRepository.GetEnabledStreamsAsync(_config.MarketDataServiceName);
	    _contractsByStream = streams
		    .Where(s => s.Contract is not null && s.DatafeedId == s.Contract.DefaultDatafeedId)
		    .ToDictionary(s => s.StreamId, s => s.Contract!);
	    _calculator = new Calculator(streams);
	    _tsw = new TradingSessionWatcher<int>(
		    _contractsByStream.Values.ToDictionary(
			    c => c.ContractId, 
			    c=> (IReadOnlyCollection<TradingSession>)c.Template.TradingSessions)
	    );

	    var constantStreams = streams
		    .Where(s => s.ConstantStreamValue is not null)
		    .Select(s => s.ConstantStreamValue!)
		    .ToList();

	    if (constantStreams.Any()) await ConfigureJobs(constantStreams);
	    
	    _logger.LogInformation($"Service started, enabled streams: {streams.Count}, contracts: {_contractsByStream.Count}, constant streams: {constantStreams.Count}");
    }
    
    private async Task ConfigureJobs(IReadOnlyCollection<ConstantStreamValue> constantStreams)
    {
	    if (constantStreams.Count == 0) return;
	    
	    var scheduler = await _schedulerFactory.GetScheduler();
	    foreach (var item in constantStreams.GroupBy(cs => cs.CronExpression))
	    {
		    var keyStr = $"generate-{item.Key}";
		    var jobKey = new JobKey(keyStr);
		    if (await scheduler.CheckExists(jobKey))
		    {
			    await scheduler.DeleteJob(jobKey);
		    }
	    
		    await scheduler.ScheduleJob(
			    JobBuilder.Create<GenerateBarWithConstantValueJob>()
				    .WithIdentity(jobKey)
				    .SetJobData(new JobDataMap() { { "data", item.ToList() } })
				    .Build(),
			    TriggerBuilder.Create()
				    .WithIdentity(keyStr)
				    .StartNow()
				    .WithCronSchedule(item.Key)
				    .Build()
		    );
	    }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    internal Dictionary<int, Stream> ActiveStreams => _calculator.ActiveStreams;
    internal Dictionary<int, ExchangeBar> GetCurrentAggregations() => _calculator.GetCurrentAggregations();
}