using System.ComponentModel.DataAnnotations;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Common.Interfaces.Api.Binance;

public class BinanceContractsFilter : PagingFilter
{
    [Required(ErrorMessage = "Market is required")] public BinanceMarket Market { get; set; }
    public string? Symbol { get; set; }
}