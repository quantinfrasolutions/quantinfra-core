using System.ComponentModel.DataAnnotations;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;

namespace QuantInfra.Common.Interfaces.Api.Backtesting;

public class ContractOverrideModel
{
    public bool OverrideAllContracts { get; set; } = false; 
    public SecurityType SecurityType { get; set; } = SecurityType.Stock;
    public PnLCalculatorType PnLCalculatorType { get; set; } = PnLCalculatorType.Default;
    public int SettlementCurrencyDecimals { get; set; } = 2;
    public decimal CostPerShare { get; set; } = 0m;
    public decimal FloatingCost { get; set; } = 0m;
    
    [Required(ErrorMessage = "Tick size is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "Tick size must be between 0.00000001 and decimal.MaxValue")]
    public decimal TickSize { get; set; } = 0.01m;
    
    [Required(ErrorMessage = "Tick value is required")]
    [Range(0.00000001, (double)decimal.MaxValue, ErrorMessage = "Tick value must be between 0.00000001 and decimal.MaxValue")]
    public decimal TickValue { get; set; } = 0.01m;
    
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

    public ContractOverride ToSdk() => new()
    {
        OverrideAllContracts = OverrideAllContracts,
        SecurityType = SecurityType,
        PnLCalculatorType = PnLCalculatorType,
        SettlementCurrencyDecimals = SettlementCurrencyDecimals,
        CostPerShare = CostPerShare,
        FloatingCost = FloatingCost,
        TickSize = TickSize,
        TickValue = TickValue,
        MinSize = MinSize,
        MinSizeMoney = MinSizeMoney,
        MaxSize = MaxSize,
        MaxSizeMoney = MaxSizeMoney,
        SizeIncrement = SizeIncrement,
    };

    public static ContractOverrideModel? FromSdk(ContractOverride co) => new()
    {
        OverrideAllContracts = co.OverrideAllContracts,
        SecurityType = co.SecurityType,
        PnLCalculatorType = co.PnLCalculatorType,
        SettlementCurrencyDecimals = co.SettlementCurrencyDecimals,
        CostPerShare = co.CostPerShare,
        FloatingCost = co.FloatingCost,
        TickSize = co.TickSize,
        TickValue = co.TickValue,
        MinSize = co.MinSize,
        MinSizeMoney = co.MinSizeMoney,
        MaxSize = co.MaxSize,
        MaxSizeMoney = co.MaxSizeMoney,
        SizeIncrement = co.SizeIncrement,
    };
}