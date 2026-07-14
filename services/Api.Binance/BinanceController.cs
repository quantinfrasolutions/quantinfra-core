using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.Binance;
using QuantInfra.Common.MarketData.Infrastructure;
using QuantInfra.Common.Utils.Collections;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Databases.Main;
using QuantInfra.Databases.Main.Models.StaticData;

namespace QuantInfra.Services.Api.Binance;

[ApiController]
[Route("api/binance")]
public class BinanceController(
    IBinanceStaticDataClient staticDataClient, 
    MainContext context,
    IMarketDataClientsRegistry<BinanceUsdmMarketDataSubscriptionRequest, BinanceUsdmMarketDataSubscription> usdmMDRegistry,
    IMarketDataClientsRegistry<BinanceUsdmOrderBookSubscriptionRequest, BinanceUsdmOrderBookSubscription> usdmOBRegistry
) : Controller
{
    [HttpGet, Route("contracts")]
    [EndpointName(nameof(GetBinanceContracts))]
    [Produces("application/json")]
    public async Task<IEnumerable<BinanceContractListView>> GetBinanceContracts([FromQuery] BinanceContractsFilter? filter = null)
    {
        filter ??= new();

        var data = await staticDataClient.GetContractsAsync(filter.Market);
        var filtered = data
            .Where(c => string.IsNullOrEmpty(filter.Symbol) || c.Symbol.ToLower().Contains(filter.Symbol.ToLower()))
            .OrderBy(x => x.Symbol)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToList();

        var brokerId = GetBrokerId(filter.Market);
        var exchangeId = GetExchangeId(filter.Market);

        var tickers = filtered.Select(x => x.Symbol.ToUpper()).ToList();
        var mapped = (await context.Contracts
            .Where(c => 
                c.Template.Broker.BrokerId == brokerId 
                && !string.IsNullOrEmpty(c.ExternalContractId) 
                && tickers.Contains(c.ExternalContractId.ToUpper())
            )
            .ToListAsync()).Where(x => !string.IsNullOrEmpty(x.ExternalContractId)).ToDictionary(x => x.ExternalContractId!);

        return filtered.Select(x =>
        {
            var c = mapped.GetValueOrDefault(x.Symbol);
            return new BinanceContractListView(x, c?.ContractId, c?.Ticker);
        }).OrderBy(x => x.BinanceContract.Symbol);
    }

    [HttpGet, Route("usdm/md")]
    [EndpointName(nameof(GetUsdmMarketDataClients))]
    [Produces("application/json")]
    public Task<IEnumerable<string>> GetUsdmMarketDataClients()
    {
        return Task.FromResult(usdmMDRegistry.GetAvailableClients().AsEnumerable());
    }
    
    [HttpGet, Route("usdm/ob")]
    [EndpointName(nameof(GetUsdmOrderBookClients))]
    [Produces("application/json")]
    public Task<IEnumerable<string>> GetUsdmOrderBookClients()
    {
        return Task.FromResult(usdmOBRegistry.GetAvailableClients().AsEnumerable());
    }

    [HttpGet, Route("usdm/md/subscriptions")]
    [EndpointName(nameof(GetBinanceUsdmMarketDataSubscriptions))]
    [Produces("application/json")]
    public async Task<IEnumerable<BinanceUsdmMarketDataSubscriptionView>> GetBinanceUsdmMarketDataSubscriptions()
    {
        var databaseSubscriptions = (
            await context
                .BinanceUsdmMarketDataSubscriptions
                .Select(s => new BinanceUsdmMarketDataSubscriptionListView(s,
                    context.Streams.SingleOrDefault(x => x.StreamId == s.StreamId)))
                .AsNoTracking()
                .ToListAsync()
            ).ToList();

        var clients = databaseSubscriptions.Select(x => x.ClientName).Distinct().ToList();

        var inMemorySubscriptions = clients.SelectMany(c =>
        {
            var client = usdmMDRegistry.GetMarketDataClient(c);
            if (client is null)
                return Array.Empty<BinanceUsdmMarketDataSubscription>();

            return client.GetActiveSubscriptions();
        }).ToList();
    
        var result = databaseSubscriptions.FullOuterJoin(
            inMemorySubscriptions,
            s => s.SubscriptionId,
            s => s.SubscriptionId,
            (d, m, id) => new BinanceUsdmMarketDataSubscriptionView(d, m)
        );

        return result;
    }

    [HttpPost, Route("usdm/md/subscriptions/{clientName}")]
    [EndpointName(nameof(CreateBinanceUsdmMarketDataSubscription))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBinanceUsdmMarketDataSubscription([FromRoute] string clientName, [FromBody] BinanceUsdmMarketDataSubscriptionRequest request)
    {
        var client = usdmMDRegistry.GetMarketDataClient(clientName);
        if (client is null) return NotFound($"Client {clientName} not found or not started");
        
        if (string.IsNullOrEmpty(request.Symbol)) ModelState.AddModelError(nameof(request.Symbol), "Symbol is required");
        else
        {
            var symbols = (await GetBinanceContracts(new() { Market = BinanceMarket.UsdmFutures, Symbol = request.Symbol }))
                .ToList();
            if (!symbols.Any(s =>
                    s.BinanceContract.Symbol.Equals(request.Symbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                ModelState.AddModelError(nameof(request.Symbol), "Symbol not found");
            }
        }

        if (request.StreamId.HasValue)
        {
            var stream = await context.Streams.SingleOrDefaultAsync(x => x.StreamId == request.StreamId.Value);
            if (stream is null) ModelState.AddModelError(nameof(request.StreamId), $"Stream {request.StreamId} not found");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        await client.Subscribe(request);
        return Ok();
    }
    
    [HttpDelete, Route("usdm/md/subscriptions/{clientName}/{subscriptionId:int}")]
    [EndpointName(nameof(DeleteBinanceUsdmMarketDataSubscription))]
    public async Task<IActionResult> DeleteBinanceUsdmMarketDataSubscription([FromRoute] string clientName, [FromRoute] int subscriptionId)
    {
        var client = usdmMDRegistry.GetMarketDataClient(clientName);
        if (client is null) return NotFound($"Client {clientName} not found or not started");

        await client.Unsubscribe(subscriptionId);
        return Ok();
    }
    
    [HttpGet, Route("usdm/ob/subscriptions")]
    [EndpointName(nameof(GetBinanceUsdmOrderBookSubscriptions))]
    [Produces("application/json")]
    public async Task<IEnumerable<BinanceUsdmOrderBookSubscriptionListView>> GetBinanceUsdmOrderBookSubscriptions()
    {
        throw new NotImplementedException();
    }

    private static int GetExchangeId(BinanceMarket market) => market switch
    {
        BinanceMarket.UsdmFutures => ExchangeConfiguration.BinanceUsdmExchangeId,
        _ => throw new NotSupportedException()
    };

    private static int GetBrokerId(BinanceMarket market) => market switch
    {
        BinanceMarket.UsdmFutures => BrokerConfiguration.BinanceUsdmBrokerId,
        _ => throw new NotSupportedException()
    };
}