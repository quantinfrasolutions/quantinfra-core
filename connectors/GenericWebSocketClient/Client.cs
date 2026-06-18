using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenericWebSocketClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Connectors.Common;
using QuantInfra.Connectors.Common.Metrics;

namespace QuantInfra.GenericWebSocketClient;

public abstract class Client : IHostedService
{
    protected readonly ILogger Logger;
    private readonly BaseConfig _config;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private bool _stopRequested;
    private readonly SemaphoreSlim _closeSempaphore = new(1, 1);
    
    protected ClientWebSocket WebSocket { get; private init; }
    
    private readonly JsonSerializerOptions _serializerOptions = new ()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
    };


    public Client(BaseConfig config, ILogger logger)
    {
        _config = config;
        Logger = logger;
        
        WebSocket = new ClientWebSocket();
        WebSocket.Options.KeepAliveInterval = config.KeepAliveInterval;
        WebSocket.Options.KeepAliveTimeout = config.KeepAliveTimeout;
    }
    
    protected abstract void ProcessMessage(IngressMessage message);
    
    protected virtual Task OnBeforeStartAsync() => Task.CompletedTask;
    protected abstract Task OnAfterWebSocketConnectedAsync();
    protected abstract void OnStop();

    public bool IsConnected() => WebSocket.State == WebSocketState.Open;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await OnBeforeStartAsync();
        
        // StartParseWorkers(_config.ReadersCount, _channel.Reader, cancellationToken);
        
        var uri = await GetUri();
        await WebSocket.ConnectAsync(uri, CancellationToken.None);
        Logger.LogInformation($"Connected to {uri}");
        
        _ = Task.Run(() => DoClientWebSocket(cancellationToken), cancellationToken);

        await OnAfterWebSocketConnectedAsync();
        Logger.LogInformation("Client started");
    }
    
    protected virtual Task<Uri> GetUri() => Task.Run(() => new Uri($"{_config.Uri}"));

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping web socket");
        if (_stopRequested)
        {
            throw new InvalidOperationException("StopAsync called twice");
        }
        _stopRequested = true;
        
        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stop requested", cancellationToken);
        var timeout = Task.Run(() => Task.Delay(10000, cancellationToken), cancellationToken);
        if (await Task.WhenAny(
                timeout,
                Task.Run(async () => await _closeSempaphore.WaitAsync(cancellationToken), cancellationToken)
            ) == timeout)
        {
            Logger.LogError("Websocket connection not closed within 10 seconds, aborting");
            WebSocket.Abort();
        }
        WebSocket.Dispose();
        OnStop();
    }
    
    private async Task DoClientWebSocket(CancellationToken ct)
    {
        // Scratch buffer for each ReceiveAsync chunk
        byte[] scratch = ArrayPool<byte>.Shared.Rent(_config.BufferSize);

        // Accumulator for a single WS message (pooled grows as needed)
        byte[] acc = ArrayPool<byte>.Shared.Rent(_config.BufferSize);
        int accLen = 0;

        try
        {
            while (!ct.IsCancellationRequested && WebSocket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(scratch);
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(segment, ct); //.ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                // Ensure accumulator capacity
                if (accLen + result.Count > acc.Length)
                {
                    int newSize = Math.Max(accLen + result.Count, acc.Length * 2);
                    byte[] newAcc = ArrayPool<byte>.Shared.Rent(newSize);
                    Buffer.BlockCopy(acc, 0, newAcc, 0, accLen);
                    ArrayPool<byte>.Shared.Return(acc);
                    acc = newAcc;
                }

                // Append chunk
                Buffer.BlockCopy(scratch, 0, acc, accLen, result.Count);
                accLen += result.Count;

                // Full message assembled?
                if (!result.EndOfMessage)
                    continue;

                var receivedAt = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
                var swReceivedAt = MetricsUtils.GetUnixMicro();
                // Transfer ownership of accumulated bytes to the queue:
                // rent exact-ish buffer (optional) or pass acc as-is.
                // Passing acc as-is is fastest; we just "swap" accumulator for a fresh one.
                var msg = new IngressMessage(acc, accLen, receivedAt, swReceivedAt);

                // Swap accumulator immediately (so receive loop can continue)
                acc = ArrayPool<byte>.Shared.Rent(64 * 1024);
                accLen = 0;

                ProcessMessage(msg);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in client web socket");
        }
        finally
        {
            // Return scratch
            ArrayPool<byte>.Shared.Return(scratch);

            // Return current accumulator (might be the fresh one or partially filled)
            ArrayPool<byte>.Shared.Return(acc);

            // _channel.Writer.TryComplete();
        }
    }

    protected void SendMessage(object message) => SendMessageAsync(message).RunSynchronously();
    
    protected async Task SendMessageAsync(object message)
    {
        await _sendSemaphore.WaitAsync();
        if (WebSocket.State != WebSocketState.Open || _stopRequested) throw new WebSocketNotConnectedException();
        var payload = JsonSerializer.Serialize(message);
        Logger.LogDebug($"Sending message {payload}");
        await WebSocket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
        _sendSemaphore.Release();
    }
}