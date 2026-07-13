using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.Binance;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Databases.Main;
using QuantInfra.Databases.Main.Models.StaticData;

namespace QuantInfra.Services.Api.Binance;

[ApiController]
[Route("api/binance")]
public class BinanceController(IBinanceStaticDataClient client, MainContext context) : Controller
{
    [HttpGet, Route("contracts")]
    [EndpointName(nameof(GetBinanceContracts))]
    [Produces("application/json")]
    public async Task<IEnumerable<BinanceContractListView>> GetBinanceContracts([FromQuery] BinanceContractsFilter? filter = null)
    {
        filter ??= new();

        var data = await client.GetContractsAsync(filter.Market);
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