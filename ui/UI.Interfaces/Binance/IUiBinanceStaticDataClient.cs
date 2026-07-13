using QuantInfra.Common.Interfaces.Api.Binance;

namespace UI.Interfaces.Binance;

public interface IUiBinanceStaticDataClient
{
    Task<IEnumerable<BinanceContractListView>> GetContracts(BinanceContractsFilter filter);
}