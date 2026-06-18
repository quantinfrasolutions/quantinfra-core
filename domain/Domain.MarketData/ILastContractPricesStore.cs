using System.Collections.Generic;

namespace QuantInfra.Domain.MarketData;

public interface ILastContractPricesStore
{
    Dictionary<int, LastPrice> LastPrices { get; }
}