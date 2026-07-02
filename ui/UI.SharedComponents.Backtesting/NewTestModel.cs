using System.ComponentModel.DataAnnotations;
using NodaTime;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;

namespace UI.SharedComponents.Backtesting;

internal class NewTestModel
{
    // TestUnit
    [Required] public string Action { get; set; }
    public string? Data { get; set; }

    public Dictionary<string, Contract> ContractOverrides { get; set; } = new();
    public Dictionary<int, string> ContractsMap { get; set; } = new();
    
    public TestExecutorOptionsModel TestExecutorOptions { get; set; } = new();
    public PersistOptionsModel PersistOptions { get; set; } = new();
    
    public TestUnit ToTestUnit() => new(Action, TestExecutorOptions.ToSdk(), PersistOptions.ToSdk(), Data, SystemClock.Instance.GetCurrentInstant(),
        ContractOverrides, ContractsMap); 
}