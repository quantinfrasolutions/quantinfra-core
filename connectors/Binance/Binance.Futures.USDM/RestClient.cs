using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Binance.Futures.USDM;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Binance.Futures.USDM.Client;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public class RestClient
{
    private readonly TradingClientConfig _config;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions? _options;
    private readonly BinanceClient _client;

    public RestClient(TradingClientConfig config, ILoggerFactory loggerFactory)
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
            ApiSecret = _config.ApiSecret,
            Logger = _logger,
        };
    }
    
    public async Task<string> GetListenKey()
    {
        var res = await _client.GetListenKeyAsync(new());
        return res.ListenKey;
    }

    public Task<BinanceOrder> PlaceOrder(NewOrderSingleExternal nos)
    {
        var request = new NewOrderRequest
        {
            NewClientOrderId = nos.OrderId.ToString(),
            Symbol = nos.ExternalContractId,
            Side = nos.Side.ToBinanceString(),
            Type = nos.OrdType.ToBinanceString(),
            Quantity = nos.OrderQty.ToString(CultureInfo.InvariantCulture),
            Timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        };

        if (nos.OrdType == OrdType.Limit)
        {
            request.TimeInForce = nos.TimeInForce.ToBinanceString();
        }
        
        if (nos.Price.HasValue)
        {
            request.Price = nos.Price.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (nos.StopPx.HasValue)
        {
            request.StopPrice = nos.StopPx.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (nos.ExpireDt.HasValue)
        {
            request.GoodTillDate = nos.ExpireDt.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        }
        
        return _client.PlaceOrderAsync(true, request);
    }
    
    public Task<BinanceOrder> CancelOrder(OrderCancelRequestExternal ocr)
    {
        var request = new CancelOrderRequest
        {
            Symbol = ocr.ExternalContractId,
            Timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrEmpty(ocr.ExternalOrderId)) request.OrderId = ocr.ExternalOrderId;
        else if (ocr.OrderId.HasValue) request.OrigClientOrderId = ocr.OrderId.ToString();
        else throw new OrderIdNotProvidedException("Either ExternalOrderId or OrderId must be provided");
        
        return _client.CancelOrderAsync(true, request);
    }

    public Task<BinanceOrder> ModifyOrder(OrderReplaceRequestExternal ocr)
    {
        if (!ocr.Price.HasValue || !ocr.OrderQty.HasValue || !ocr.Side.HasValue)
        {
            throw new InvalidModifyRequestException("Price, OrderQty, and Side must be provided");
        }
        var request = new ModifyOrderRequest
        {
            Symbol = ocr.ExternalContractId,
            Timestamp = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Price = ocr.Price.Value.ToString(CultureInfo.InvariantCulture),
            Quantity = ocr.OrderQty.Value.ToString(CultureInfo.InvariantCulture),
            Side = ocr.Side.Value.ToBinanceString(),
        };
        
        if (!string.IsNullOrEmpty(ocr.ExternalOrderId)) request.OrderId = ocr.ExternalOrderId;
        else if (ocr.OrderId.HasValue) request.OrigClientOrderId = ocr.OrderId.ToString();
        else throw new OrderIdNotProvidedException("Either ExternalOrderId or OrderId must be provided");
        
        return _client.ModifyOrderAsync(true, request);
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