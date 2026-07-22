using System.Collections.Concurrent;
using Common.Metrics;
using GenericWebSocketClient;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

internal enum OrderCommandKind
{
    Place,
    Cancel,
    Modify,
}

internal sealed record PendingOrderCommand(OrderCommandKind Kind, object Request);

internal sealed record OrderCommandFailure(
    PendingOrderCommand Command,
    int? ErrorCode,
    string ErrorMessage,
    Exception? Exception,
    long ReceivedAt,
    long SwReceivedAt);

internal sealed class OrderWebSocketClient : GenericWebSocketClient.Client
{
    private readonly TradingClientConfig _config;
    private readonly string _apiSecret;
    private readonly Action<OrderCommandFailure> _failureHandler;
    private readonly ConcurrentDictionary<string, PendingOrderCommand> _pendingCommands = new();

    public OrderWebSocketClient(
        TradingClientConfig config,
        string apiSecret,
        ILoggerFactory loggerFactory,
        Action<OrderCommandFailure> failureHandler)
        : base(config, loggerFactory.CreateLogger<OrderWebSocketClient>())
    {
        _config = config;
        _apiSecret = apiSecret;
        _failureHandler = failureHandler;
    }

    public void PlaceOrder(NewOrderSingleExternal order) =>
        Submit(new NewOrder(order, _config.ApiKey, _apiSecret), new PendingOrderCommand(OrderCommandKind.Place, order));

    public void CancelOrder(OrderCancelRequestExternal request) =>
        Submit(new CancelOrder(request, _config.ApiKey, _apiSecret), new PendingOrderCommand(OrderCommandKind.Cancel, request));

    public void ModifyOrder(OrderReplaceRequestExternal request) =>
        Submit(new ModifyOrder(request, _config.ApiKey, _apiSecret), new PendingOrderCommand(OrderCommandKind.Modify, request));

    protected override Task<Uri> GetUri() => Task.FromResult(ResolveUri(_config));

    internal static Uri ResolveUri(TradingClientConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.WebSocketApiUri)) return new Uri(config.WebSocketApiUri);

        var restUri = new Uri(config.RestUri);
        if (restUri.Host.Equals("fapi.binance.com", StringComparison.OrdinalIgnoreCase))
            return new Uri("wss://ws-fapi.binance.com/ws-fapi/v1");
        if (restUri.Host.Contains("testnet", StringComparison.OrdinalIgnoreCase))
            return new Uri("wss://testnet.binancefuture.com/ws-fapi/v1");

        throw new InvalidOperationException(
            $"Cannot infer the Binance WebSocket API endpoint from REST host '{restUri.Host}'. Configure WebSocketApiUri explicitly.");
    }

    protected override void ProcessMessage(IngressMessage message)
    {
        if (!OrderCommandResponse.TryParse(
                new ReadOnlyMemory<byte>(message.Buffer, 0, message.Length), out var response))
        {
            Logger.LogWarning("Received an unrecognized Binance order WebSocket response");
            return;
        }

        if (!_pendingCommands.TryRemove(response.RequestId, out var pending))
        {
            Logger.LogWarning("Received a Binance order WebSocket response for unknown request {RequestId}", response.RequestId);
            return;
        }

        if (response.IsSuccess)
        {
            Logger.LogDebug("Binance accepted {CommandKind} request {RequestId}", pending.Kind, response.RequestId);
            return;
        }

        _failureHandler(new OrderCommandFailure(
            pending,
            response.ErrorCode,
            response.ErrorMessage ?? $"Binance WebSocket API returned status {response.Status}",
            null,
            message.ReceivedAt,
            message.SwReceivedAt));
    }

    protected override Task OnAfterWebSocketConnectedAsync() => Task.CompletedTask;

    protected override void OnStop() => _pendingCommands.Clear();

    protected override void LogOutgoingMessage(object message, string payload)
    {
        if (message is OrderCommand command)
        {
            Logger.LogDebug("Sending Binance order command {Method} with request id {RequestId}", command.Method, command.RequestId);
            return;
        }

        base.LogOutgoingMessage(message, payload);
    }

    private void Submit(OrderCommand command, PendingOrderCommand pending)
    {
        if (!_pendingCommands.TryAdd(command.RequestId, pending))
            throw new InvalidOperationException($"Duplicate Binance WebSocket request id {command.RequestId}");

        _ = SendAndHandleFailureAsync(command);
    }

    private async Task SendAndHandleFailureAsync(OrderCommand command)
    {
        try
        {
            await SendMessageAsync(command);
        }
        catch (Exception exception)
        {
            if (!_pendingCommands.TryRemove(command.RequestId, out var pending)) return;

            var now = SystemClock.Instance.GetCurrentInstant();
            _failureHandler(new OrderCommandFailure(
                pending,
                null,
                exception.Message,
                exception,
                now.ToUnixTimeMilliseconds(),
                MetricsUtils.GetUnixMicro()));
        }
    }
}
