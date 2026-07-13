using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.StaticDataClient.Internal;
using QuantInfra.Connectors.Binance.StaticDataClient.Models;

[assembly:InternalsVisibleTo("QuantInfra.Connectors.Binance.StaticDataClient.Tests")]

namespace QuantInfra.Connectors.Binance.StaticDataClient;

public sealed class BinanceStaticDataClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly BinanceStaticDataClientOptions _options;

    public BinanceStaticDataClient() : this(new(), new())
    {
    }

    private BinanceStaticDataClient(HttpClient? httpClient, BinanceStaticDataClientOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new BinanceStaticDataClientOptions();
    }

    internal static BinanceStaticDataClient CreateClient(HttpClient httpClient) => new(httpClient, null);

    public Task<BinanceExchangeInfo> GetExchangeInfoAsync(CancellationToken cancellationToken = default) =>
        GetExchangeInfoAsync(_options.DefaultMarket, cancellationToken);

    public async Task<BinanceExchangeInfo> GetExchangeInfoAsync(
        BinanceMarket market,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.GetExchangeInfoUri(market));
        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new BinanceStaticDataException(market, response.StatusCode, Limit(body, 8_192));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var raw = await JsonSerializer.DeserializeAsync<ExchangeInfoResponse>(
            stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (raw is null)
            throw new JsonException($"Binance {market} returned an empty exchange-info response.");

        return Map(market, raw);
    }

    public async Task<IReadOnlyList<BinanceAsset>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        (await GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false)).Assets;

    public async Task<IReadOnlyList<BinanceAsset>> GetAssetsAsync(
        BinanceMarket market, CancellationToken cancellationToken = default) =>
        (await GetExchangeInfoAsync(market, cancellationToken).ConfigureAwait(false)).Assets;

    public async Task<IReadOnlyList<BinanceContract>> GetContractsAsync(CancellationToken cancellationToken = default) =>
        (await GetExchangeInfoAsync(cancellationToken).ConfigureAwait(false)).Contracts;

    public async Task<IReadOnlyList<BinanceContract>> GetContractsAsync(
        BinanceMarket market, CancellationToken cancellationToken = default) =>
        (await GetExchangeInfoAsync(market, cancellationToken).ConfigureAwait(false)).Contracts;

    private static BinanceExchangeInfo Map(BinanceMarket market, ExchangeInfoResponse response)
    {
        var contracts = response.Symbols.Select(symbol => MapContract(market, symbol)).ToArray();
        var assetRoles = new Dictionary<string, AssetRoles>(StringComparer.OrdinalIgnoreCase);

        foreach (var contract in contracts)
        {
            AddRole(contract.BaseAsset, isBase: true);
            AddRole(contract.QuoteAsset, isQuote: true);
            AddRole(contract.MarginAsset, isMargin: true);
            AddRole(contract.SettlementAsset, isMargin: true);
        }

        foreach (var asset in response.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Asset))
                continue;

            var roles = GetOrAdd(asset.Asset);
            roles.IsMargin = true;
            roles.IsMarginAvailable = asset.MarginAvailable;
            roles.AutoAssetExchange = ParseDecimal(asset.AutoAssetExchange);
        }

        var assets = assetRoles
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new BinanceAsset(x.Key, market, x.Value.IsBase, x.Value.IsQuote,
                x.Value.IsMargin, x.Value.IsMarginAvailable, x.Value.AutoAssetExchange))
            .ToArray();

        return new BinanceExchangeInfo(market, FromUnixMilliseconds(response.ServerTime), assets, contracts);

        AssetRoles GetOrAdd(string symbol)
        {
            if (!assetRoles.TryGetValue(symbol, out var roles))
                assetRoles.Add(symbol, roles = new AssetRoles());
            return roles;
        }

        void AddRole(string? symbol, bool isBase = false, bool isQuote = false, bool isMargin = false)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return;
            var roles = GetOrAdd(symbol);
            roles.IsBase |= isBase;
            roles.IsQuote |= isQuote;
            roles.IsMargin |= isMargin;
        }
    }

    private static BinanceContract MapContract(BinanceMarket market, ExchangeSymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol.Symbol))
            throw new JsonException("A Binance exchange-info symbol has no symbol identifier.");
        if (string.IsNullOrWhiteSpace(symbol.BaseAsset))
            throw new JsonException($"Binance symbol {symbol.Symbol} has no base asset.");
        if (string.IsNullOrWhiteSpace(symbol.QuoteAsset))
            throw new JsonException($"Binance symbol {symbol.Symbol} has no quote asset.");

        return new BinanceContract
        {
            Market = market,
            Symbol = symbol.Symbol,
            Pair = symbol.Pair,
            Status = symbol.Status ?? symbol.ContractStatus,
            ContractType = symbol.ContractType,
            BaseAsset = symbol.BaseAsset,
            QuoteAsset = symbol.QuoteAsset,
            MarginAsset = symbol.MarginAsset,
            SettlementAsset = symbol.SettleAsset ?? symbol.MarginAsset ?? symbol.QuoteAsset,
            OnboardDate = FromUnixMilliseconds(symbol.OnboardDate),
            DeliveryDate = FromUnixMilliseconds(symbol.DeliveryDate),
            ContractSize = symbol.ContractSize,
            PricePrecision = symbol.PricePrecision,
            QuantityPrecision = symbol.QuantityPrecision,
            BaseAssetPrecision = symbol.BaseAssetPrecision,
            QuoteAssetPrecision = symbol.QuoteAssetPrecision ?? symbol.QuotePrecision,
            UnderlyingType = symbol.UnderlyingType,
            UnderlyingSubTypes = symbol.UnderlyingSubTypes,
            OrderTypes = symbol.OrderTypes,
            TimeInForce = symbol.TimeInForce,
            Filters = symbol.Filters
                .Where(x => !string.IsNullOrWhiteSpace(x.FilterType))
                .Select(x => new BinanceSymbolFilter(x.FilterType!, x.Values.ToDictionary(
                    pair => pair.Key,
                    pair => JsonValueToString(pair.Value),
                    StringComparer.OrdinalIgnoreCase)))
                .ToArray()
        };
    }

    private static DateTimeOffset? FromUnixMilliseconds(long? value) =>
        value is > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static string JsonValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };

    private static string Limit(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed class AssetRoles
    {
        public bool IsBase { get; set; }
        public bool IsQuote { get; set; }
        public bool IsMargin { get; set; }
        public bool? IsMarginAvailable { get; set; }
        public decimal? AutoAssetExchange { get; set; }
    }
}
