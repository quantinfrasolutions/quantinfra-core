using System.ComponentModel.DataAnnotations;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class CreateStrategyRequest
{
    [Required(ErrorMessage = "Name is required")]
    [Length(1, 1024, ErrorMessage = "Name must be between 1 and 1024 characters long")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "ClassName is required")]
    public string ClassName { get; set; }
    
    public string Params { get; set; }
    
    [Required(ErrorMessage = "RequiredBarStorages are required")]
    public Dictionary<string, BarStorageConfig> RequiredBarStorages { get; set; }
    
    [Required(ErrorMessage = "Symbols are required")]
    public Dictionary<string, int> Symbols { get; set; }
    
    // public LiquidationParameters? LiquidationParameters { get; set; }
    
    [Required(ErrorMessage = "Account is required")]
    public CreateAccountRequest Account { get; set; }
    public bool StartImmediately { get; set; }
    public bool UseSignalGroups { get; set; }
    
    [Required(ErrorMessage = "StrategyServiceName is required")]
    public string StrategyServiceName { get; set; }

    public StrategyConfig ToStrategyConfig() => new StrategyConfig(
        string.IsNullOrEmpty(Name) ? string.Empty : Name,
        ClassName,
        Params,
        RequiredBarStorages,
        Symbols,
        null, // LiquidationParameters,
        UseSignalGroups
    );
}