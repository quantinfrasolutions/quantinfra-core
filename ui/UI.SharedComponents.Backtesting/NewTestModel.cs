using System.ComponentModel.DataAnnotations;
using NodaTime;
using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;

namespace UI.SharedComponents.Backtesting;

internal class NewTestModel
{
    // TestUnit
    [Required] public string Action { get; set; }
    public string? Data { get; set; }
    
    public string? Calculator { get; set; }
    public string? CalculatorData { get; set; }
    
    public ContractOverrideModel? ContractOverride { get; set; } = null;
    public TestExecutorOptionsModel TestExecutorOptions { get; set; } = new();
    public PersistOptionsModel PersistOptions { get; set; } = new();
    
    public TestUnit ToTestUnit() => new(Action, TestExecutorOptions.ToSdk(), PersistOptions.ToSdk(), 
        Calculator, Data, CalculatorData, SystemClock.Instance.GetCurrentInstant(), ContractOverride?.ToSdk()); 
}