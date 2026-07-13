using System.ComponentModel.DataAnnotations;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateContractTemplateRequest
{
    [Required(ErrorMessage = "Template name is required")] public string Name { get; set; }
    [Required(ErrorMessage = "Security type iis required")] public SecurityType SecurityType { get; set; } = SecurityType.Stock;
    public PnLCalculatorType PlCalculatorType { get; set; } = PnLCalculatorType.Default;
    public int? AssetId { get; set; }
    
    [Required(ErrorMessage = "MinSize is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "MinSize must be between 0.00000001 and decimal.MaxValue")] 
    public decimal MinSize { get; set; } = 1;
    public decimal? MinSizeMoney { get; set; }
    
    [Required(ErrorMessage = "MaxSize is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "MaxSize must be between 0.00000001 and decimal.MaxValue")]
    public decimal MaxSize { get; set; } = 1000000m;
    public decimal? MaxSizeMoney { get; set; }
    
    [Required(ErrorMessage = "Size increment is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "Size increment must be between 0.00000001 and decimal.MaxValue")]
    public decimal SizeIncrement { get; set; } = 1;
    
    [Required(ErrorMessage = "Tick size is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "Tick size must be between 0.00000001 and decimal.MaxValue")]
    public decimal TickSize { get; set; } = 0.01m;
    public decimal? TickValue { get; set; }
    public decimal PriceQuotation { get; set; } = 1;

    [Required(ErrorMessage = "Settlement currency is required")]
    public int SettlementCurrencyId { get; set; } = 840;
    public int? BaseCurrencyId { get; set; }
    public int? DefaultDatafeedId { get; set; }
    public List<int> CommissionIds { get; set; } = new();
    public List<int> TradingSessionsIds { get; set; } = new();
    
    [Required(ErrorMessage = "Exchange is required")]
    public int ExchangeId { get; set; } // TODO: contract may be listed on several exchanges, with its own commission structures and trading sessions
    
    [Required(ErrorMessage = "Broker is required")]
    public int BrokerId { get; set; } // TODO: contract may be provided by several brokers, with its own commission structures
    public int DaysInYear { get; set; } = 252;
    public string? Description { get; set; }
}
