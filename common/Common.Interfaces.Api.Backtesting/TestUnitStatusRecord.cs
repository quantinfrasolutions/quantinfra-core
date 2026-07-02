using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.Backtesting;

public class TestUnitStatusRecord : QuantInfra.Sdk.Backtesting.TestUnit
{
    [JsonConstructor]
    public TestUnitStatusRecord() { }
    
    public TestUnitStatusRecord(
        Guid testId,
        string action,
        TestExecutorOptions options,
        PersistOptions persistOptions,
        IReadOnlyDictionary<string, Contract> contractOverrides,
        IReadOnlyDictionary<int, string> contractsMap,
        Instant createdAt,
        string data,
        TestUnitStatus status,
        string? statusMessage
    ) : base(testId, action, options, persistOptions, contractOverrides, contractsMap, createdAt, data)
    {
        Status = status;
        StatusMessage = statusMessage;
    }
    
    public TestUnitStatusRecord(TestUnit unit) : this(unit.TestId, unit.Action, unit.Options,
        unit.PersistOptions, unit.ContractOverrides, unit.ContractsMap, unit.CreatedAt, unit.Data, TestUnitStatus.Queued, null)
    { }

    public TestUnitStatus Status { get; set; }
    public string? StatusMessage { get; set; }
}