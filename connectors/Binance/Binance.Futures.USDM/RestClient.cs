using System.Text.Json;
using System.Text.Json.Serialization;
using Binance.Futures.USDM;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Binance.Futures.USDM.Client;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public class RestClient
{
    private readonly TradingClientConfig _config;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions? _options;
    private readonly BinanceClient _client;

    public RestClient(TradingClientConfig config, string apiSecret, ILoggerFactory loggerFactory)
    {
        _config = config;
        _logger = loggerFactory.CreateLogger<RestClient>();
        
        _options = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        _client = new BinanceClient(new HttpClient()
        {
            BaseAddress = new Uri(config.RestUri),
            DefaultRequestHeaders = { { "X-MBX-APIKEY", _config.ApiKey } }
        })
        {
            ApiSecret = apiSecret,
            Logger = _logger,
        };
    }
    
    public async Task<string> GetListenKey()
    {
        var res = await _client.GetListenKeyAsync(new());
        return res.ListenKey;
    }

    public async Task<bool> GetAccountPositionMode() =>
        (await _client.GetPositionModeAsync(true, SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds())).DualSidePosition;

    public Task<IEnumerable<BinanceOrder>> GetAccountRawOpenOrders() =>
        _client.GetAccountRawOpenOrdersAsync(true, SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds());

    public Task<IEnumerable<BinancePosition>> GetAccountPositionsAsync() =>
        _client.GetAccountRawPositionsAsync(true, SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds());

    public Task<IEnumerable<BalanceRecord>> GetAccountBalancesAsync() =>
        _client.GetAccountBalancesAsync(true, SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds());

    public async Task<IReadOnlyCollection<BinanceTrade>> GetAccountTrades(string symbol, Instant? from)
    {
        var timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
        var startTime = from?.ToUnixTimeMilliseconds();
        return (await _client.GetRawTradesAsync(true, timestamp, symbol, startTime)).ToList();
    }
    
    public async Task<IReadOnlyCollection<string>> GetAvailableSymbols() => 
        (await _client.GetExchangeInfoAsync()).Symbols.Select(s => s.Symbol).ToList();
}
