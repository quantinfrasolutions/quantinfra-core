using System.Text.Json;
using System.Text.Json.Serialization;
using Binance.Futures.USDM;
using Binance.Futures.USDM.Jobs;
using GenericWebSocketClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Binance.Futures.USDM.Client;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.UserStreams;
using QuantInfra.Connectors.Common.Metrics;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Infrastructure;
using QuantInfra.Sdk.Trading.Orders;
using Quartz;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public class TradingClient : GenericWebSocketClient.Client, IHostedService, IHostedTradingClient
{
    private readonly TradingClientConfig _config;
    private readonly ITradingClientResponsesHandler _responsesHandler;
    private readonly IClock _clock;
    private readonly int _accountId;
    private readonly ISchedulerFactory _schedulerFactory;
    
    private readonly RestClient _restClient;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() }
    };

    public TradingClient(
        QuantInfra.Sdk.Accounts.TradingClientConfig config, 
        IServiceProvider serviceProvider, 
        ILoggerFactory loggerFactory,
        ITradingClientResponsesHandler responsesHandler,
        IClock clock
    ) : base(config.GetParams<TradingClientConfig>()!, loggerFactory.CreateLogger<TradingClient>())
    {
        _config = config.GetParams<TradingClientConfig>()!;
        _responsesHandler = responsesHandler;
        _clock = clock;
        _accountId = config.AccountId;
        _restClient = new(_config, loggerFactory);
        _schedulerFactory = serviceProvider.GetService<ISchedulerFactory>()!;
    }

    protected override async Task OnBeforeStartAsync()
    {
        var dualSidePosition = await _restClient.GetAccountPositionMode();
        if (dualSidePosition)
        {
            throw new NotSupportedException("Hedged mode is not supported for Binance accounts");
        }
        
        await ConfigureJobs();
    }

    protected override async Task<Uri> GetUri()
    {
        var listenKey = await _restClient.GetListenKey();
        var uri = (new UriBuilder(new Uri(_config.Uri)) { Path = $"/ws/{listenKey}" }).Uri; 
        return uri;
    }

    protected override void ProcessMessage(IngressMessage message)
    {
        var document = JsonDocument.Parse(new ReadOnlyMemory<byte>(message.Buffer, 0, message.Length)); // TODO: options
        var evt = document.RootElement.GetProperty("e").GetString();
        // var ts = document.RootElement.GetProperty("E").GetInt64();

        switch (evt)
        {
            case "ORDER_TRADE_UPDATE":
                var orderUpdate = document.Deserialize<OrderUpdate>(_serializerOptions);
                ProcessOrderUpdate(orderUpdate, message.ReceivedAt, message.SwReceivedAt);
                break;
            case "ACCOUNT_UPDATE":
                var accountUpdate = document.Deserialize<AccountUpdate>(_serializerOptions);
                ProcessAccountUpdate(accountUpdate, message.ReceivedAt, message.SwReceivedAt);
                break;
            case "listenKeyExpired":
                // TODO: disconnect and reconnect
                Environment.FailFast("listenKeyExpired");
                break;
        }
    }

    private void ProcessAccountUpdate(AccountUpdate? accountUpdate, long receivedAt, long swReceivedAt)
    {
        if (accountUpdate == null) throw new InvalidOperationException("accountUpdate is null");

        var now = SystemClock.Instance.GetCurrentInstant();
        foreach (var ebo in accountUpdate.ToExternalBalanceOperation(_accountId, now))
        {
            _responsesHandler.OnBalanceOperation(ebo, receivedAt, swReceivedAt);
        }
    }

    private void ProcessOrderUpdate(OrderUpdate? orderUpdate, long receivedAt, long swReceivedAt)
    {
        if (orderUpdate == null) throw new InvalidOperationException("orderUpdate is null");
        
        Logger.LogDebug("Order update received: {order}", orderUpdate.Order);
        
        var er = orderUpdate.ToExternalExecutionReport(_accountId);
        var now = _clock.GetCurrentInstant();
        _responsesHandler.OnExecutionReport(er, receivedAt, swReceivedAt);

        if (er.ExecType == ExecType.Fill)
        {
            _responsesHandler.OnTrade(orderUpdate.ToExternalTradeRecord(_accountId), receivedAt, swReceivedAt);
        }
    }

    protected override Task OnAfterWebSocketConnectedAsync()
    {
        _responsesHandler.OnConnect(_accountId);
        return Task.CompletedTask;
    }
    
    protected override void OnStop()
    {
        
    }

    public void PlaceOrder(NewOrderSingleExternal order)
    {
        Logger.LogInformation("PlaceOrder, order={order}", order);
        var orderId = order.OrderId;
        try
        {
            var res = _restClient.PlaceOrder(order).ConfigureAwait(false).GetAwaiter().GetResult();
            // In case of successful placement of the order, the result is retrieved from the socket
            return;
        }
        catch (Exception e)
        {
            ExternalExecutionReport extEr;
            var now = _clock.GetCurrentInstant();
            var swNow = MetricsUtils.GetUnixMicro();
            if (e is SwaggerException<OrderErrorResponse> typedEx)
            {
                Logger.LogError(e, "Error placing order: code: {code}, msg: {msg}", typedEx.Result.Code, typedEx.Result.Msg);

                _responsesHandler.OnExecutionReport(
                    order.OutrightReject(now,
                        rejectReason: typedEx.Result.Code?.FromBinanceErrorCode() ?? RejectReason.NotSpecified,
                        rejectText: $"code: {typedEx.Result.Code}, msg: {typedEx.Result.Msg}"
                    ), 
                    now.ToUnixTimeMilliseconds(), 
                    swNow
                );
            }
            else
            {
                Logger.LogError(e, "Error placing order");
                _responsesHandler.OnExecutionReport(
                    order.OutrightReject(now, rejectReason: RejectReason.NotSpecified, rejectText: e.Message),
                    now.ToUnixTimeMilliseconds(), swNow);
            }
        }
    }

    public void CancelOrder(OrderCancelRequestExternal ocr)
    {
        Logger.LogInformation($"CancelOrder, ocr={ocr}");
        try
        {
            var res = Task.Run(() => _restClient.CancelOrder(ocr)).ConfigureAwait(false).GetAwaiter().GetResult();
            Logger.LogDebug("Received REST response: {res}", res);
            // In case of successful cancellation the update received through WS is used to generate the ER
            return;
        }
        catch (Exception ex)
        {
            var now = _clock.GetCurrentInstant();
            var swNow = MetricsUtils.GetUnixMicro();
            
            if (ex is SwaggerException<OrderErrorResponse> typedEx)
            {
                if (typedEx.Result.Code == -2011) // Unknown order sent.
                {
                    // If we're trying to cancel a non-existent order, it means that the order is actually cancelled
                    _responsesHandler.OnOrderCancelReject(
                        new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.UnknownOrder, typedEx.Result.Msg),
                        now.ToUnixTimeMilliseconds(), swNow
                    );

                    return;
                }

                Logger.LogError(ex, "Error canceling order");
                _responsesHandler.OnOrderCancelReject(new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.Other, typedEx.Result.Msg),
                    now.ToUnixTimeMilliseconds(), swNow);
                return;
            }

            Logger.LogError(ex, "Error canceling order");
            Logger.LogError(ex, "Error canceling order");
            _responsesHandler.OnOrderCancelReject(new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.Other, ex.Message),
                now.ToUnixTimeMilliseconds(), swNow);
        }
    }
    
    public void ReplaceOrder(OrderReplaceRequestExternal ocr)
    {
        Logger.LogInformation($"ReplaceOrder, ocr={ocr}");
        try
        {
            var res = Task.Run(() => _restClient.ModifyOrder(ocr)).ConfigureAwait(false).GetAwaiter().GetResult();
            Logger.LogInformation($"Received REST response: {res}");
            // In case of successful cancellation the update received through WS is used to generate the ER
            return;
        }
        catch (Exception ex)
        {
            var now = _clock.GetCurrentInstant();
            var swNow = MetricsUtils.GetUnixMicro();
            
            if (ex is InvalidModifyRequestException)
            {
                throw new NotImplementedException();
                // TODO
            }
            if (ex is SwaggerException<OrderErrorResponse> typedEx)
            {
                if (typedEx.Result.Code == -2011    // Unknown order sent.
                    || typedEx.Result.Code == -2013 // Order does not exist
                )
                {
                    _responsesHandler.OnOrderCancelReject(
                        new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.UnknownOrder, typedEx.Result.Msg),
                        now.ToUnixTimeMilliseconds(), swNow
                    );
                    return;
                }

                if (typedEx.Result.Code == -5027) // No need to modify the order
                {
                    // The order is already modified
                    _responsesHandler.OnOrderCancelReject(
                        new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.OrderUnchanged, typedEx.Result.Msg),
                        now.ToUnixTimeMilliseconds(), swNow
                    );
                    return;
                }
                
                Logger.LogError(ex, "Error replacing order");
                _responsesHandler.OnOrderCancelReject(new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.Other, typedEx.Result.Msg),
                    now.ToUnixTimeMilliseconds(), swNow);
                return;
            }
            
            Logger.LogError(ex, "Error replacing order");
            _responsesHandler.OnOrderCancelReject(new(_accountId, ocr.OrderId, ocr.ExternalOrderId, string.Empty, CxlRejReason.Other, ex.Message),
                now.ToUnixTimeMilliseconds(), swNow);
        }
    }
    
    public void RequestAccountFullSnapshot(IReadOnlyDictionary<string, Instant>? lastReceivedTradeDts,
        Instant? lastReceivedBalanceOperationDt, Guid? requestId = null) =>
        Task.Run(() =>
        {
            try
            {
                var snapshot = GetAccountFullSnapshotAsync(lastReceivedTradeDts, lastReceivedBalanceOperationDt).ConfigureAwait(false).GetAwaiter().GetResult();
                _responsesHandler.OnFullSnapshotReceived(_accountId, true, snapshot, _clock.GetCurrentInstant().ToUnixTimeMilliseconds(), MetricsUtils.GetUnixMicro());
            }
            catch
            {
                _responsesHandler.OnFullSnapshotReceived(_accountId, false, null, _clock.GetCurrentInstant().ToUnixTimeMilliseconds(), MetricsUtils.GetUnixMicro());
            }
        });

    private Task<ExternalAccountFullSnapshot> GetAccountFullSnapshotAsync(IReadOnlyDictionary<string, Instant>? lastReceivedTradeDts, Instant? lastReceivedBalanceOperationDt) =>
        Task.Run(async () =>
        {
            var orders = await GetAccountOrdersSnapshotAsync();
            var positions = await GetAccountPositionsSnapshotAsync();
            var balances = await GetAccountBalancesSnapshotAsync();
            
            var maxHistoryDt = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(7));

            var trades = lastReceivedTradeDts == null
                ? new List<ExternalTradeRecord>()
                :   (
                        await Task.WhenAll(
                        lastReceivedTradeDts.Select(s => GetAccountRawTradesAsync(s.Key, Instant.Max(s.Value, maxHistoryDt)))
                        )
                    )
                    .SelectMany(t => t)
                    .OrderBy(t => t.Time).ThenBy(t => t.Id)
                    .Select(t => t.ToExternalTradeRecord(_accountId))
                    .ToList();

            var snapshot = new ExternalAccountFullSnapshot
            {
                AccountId = _accountId,
                Orders = orders.Orders,
                Positions = positions.Positions,
                Trades = trades,
                Balances = balances.Balances,
                UpdateTs = SystemClock.Instance.GetCurrentInstant(),
            };

            return snapshot;
        });
    
    public void RequestAccountOrdersSnapshot(Guid? requestId = null) =>
        Task.Run(async () =>
        {
            try
            {
                var orders = await GetAccountOrdersSnapshotAsync().ConfigureAwait(false);
                _responsesHandler.OnOrdersSnapshotReceived(_accountId, true, orders,
                    _clock.GetCurrentInstant().ToUnixTimeMilliseconds(), MetricsUtils.GetUnixMicro());
            }
            catch (Exception e)
            {
                _responsesHandler.OnOrdersSnapshotReceived(_accountId, false, null,
                    _clock.GetCurrentInstant().ToUnixTimeMilliseconds(), MetricsUtils.GetUnixMicro());
            }
        });
    
    private async Task<ExternalAccountOrdersSnapshot> GetAccountOrdersSnapshotAsync()
    {
        var rawOrders = await _restClient.GetAccountRawOpenOrders();
        return new ExternalAccountOrdersSnapshot()
        {
            AccountId = _accountId,
            Orders = rawOrders.Select(o => o.ToExternalExecutionReport(_accountId)).ToList(),
            UpdateTs = SystemClock.Instance.GetCurrentInstant(),
        };
    }

    private async Task<AccountPositionsSnapshot> GetAccountPositionsSnapshotAsync()
    {
        var rawPositions = await _restClient.GetAccountPositionsAsync();
        return new AccountPositionsSnapshot()
        {
            AccountId = _accountId,
            Positions = rawPositions.Select(p => p.ToExternalPositionReport(_accountId)).ToList(),
            UpdateTs = SystemClock.Instance.GetCurrentInstant(),
        };
    }

    private async Task<AccountBalancesSnapshot> GetAccountBalancesSnapshotAsync()
    {
        var rawBalances = await _restClient.GetAccountBalancesAsync();
        return new AccountBalancesSnapshot()
        {
            AccountId = _accountId,
            Balances = rawBalances.ToDictionary(b => b.Asset, b => b.Balance),
            UpdateTs = SystemClock.Instance.GetCurrentInstant(),
        };
    }

    private Task<IReadOnlyCollection<BinanceTrade>> GetAccountRawTradesAsync(string symbol, Instant fromDt) =>
        _restClient.GetAccountTrades(symbol, fromDt);
    
    
    private async Task ConfigureJobs()
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        var renewListenerKeyJobKey = new JobKey($"renewListenerKeyJob-{_accountId}");
        if (await scheduler.CheckExists(renewListenerKeyJobKey))
        {
            await scheduler.DeleteJob(renewListenerKeyJobKey);
        }
        await scheduler.ScheduleJob(
            JobBuilder.Create<RenewListenerKeyJob>()
                .WithIdentity(renewListenerKeyJobKey)
                .SetJobData(new JobDataMap() { WrappedMap =
                {
                    { RenewListenerKeyJob.AccountIdKey, _accountId },
                    { RenewListenerKeyJob.RestClientKey, _restClient }
                }})
                .Build(), 
            TriggerBuilder.Create()
                .WithIdentity($"renewListenerKeyJobTrigger-{_accountId}")
                .StartAt(DateTimeOffset.Now.Add(TimeSpan.FromMinutes(_config.RenewListenerKeyPeriodMinutes)))
                .WithSimpleSchedule(sb => 
                    sb.WithIntervalInMinutes(_config.RenewListenerKeyPeriodMinutes).RepeatForever()
                )
                .Build()
        );
    }
}