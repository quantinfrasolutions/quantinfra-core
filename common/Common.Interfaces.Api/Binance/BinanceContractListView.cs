using QuantInfra.Connectors.Binance.StaticDataClient.Models;

namespace QuantInfra.Common.Interfaces.Api.Binance;

public class BinanceContractListView
{
    public BinanceContractListView(BinanceContract binanceContract, int? contractId, string? ticker)
    {
        BinanceContract = binanceContract;
        ContractId = contractId;
        Ticker = ticker;
    }

    public BinanceContract BinanceContract { get; init; }
    public int? ContractId { get; init; }
    public string? Ticker { get; init; }
}