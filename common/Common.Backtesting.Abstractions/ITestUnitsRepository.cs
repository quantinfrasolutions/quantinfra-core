using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface ITestUnitsRepository
{
    Task<IReadOnlyCollection<TestUnitListView>> GetTestUnitsAsync(TestUnitsFilter filter);
    Task<TestUnitStatusRecord?> GetTestUnitAsync(Guid unitId);
    Task CreateTestUnitAsync(TestUnit testUnit);
    Task DeleteTestUnitAsync(Guid id);
    Task SetUnitStatus(Guid unitId, Sdk.Backtesting.TestUnitStatus status, string? statusMessage = null);
}