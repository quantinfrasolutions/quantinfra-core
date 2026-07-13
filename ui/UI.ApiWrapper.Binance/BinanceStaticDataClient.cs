using QuantInfra.Common.Interfaces.Api.Binance;
using UI.Interfaces.Binance;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

public partial class ApiRepository : IUiBinanceStaticDataClient
{
    public Task<IEnumerable<BinanceContractListView>> GetContracts(BinanceContractsFilter filter) =>
        RetrieveCollection("contracts", () => _wrapper.Client.GetBinanceContractsAsync((int)filter.Market, filter.Symbol, filter.Limit, filter.Offset));
}