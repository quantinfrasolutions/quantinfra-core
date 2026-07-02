using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.UI.Interfaces.Backtesting;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

public partial class ApiRepository : IUiTestUnitsRepository
{
    public Task<IEnumerable<TestUnitListView>> GetTestsHistory(TestUnitsFilter? filter = null) =>
        RetrieveCollection("history", () => _wrapper.Client.GetTestUnitsAsync(filter?.TestId, filter?.CreatedAtFrom, filter?.CreatedAtTo,
            filter?.Action, filter?.Limit, filter?.Offset));

    public Task CreateTestUnit(TestUnit unit) =>
        Call("Test unit created", "Failed to create a test unit", () => _wrapper.Client.CreateTestUnitAsync(unit));

    public Task<TestUnitStatusRecord> GetTestUnitStatus(Guid unitId) =>
        Retrieve("test status", () => _wrapper.Client.GetTestUnitAsync(unitId));

    public Task DeleteTestUnit(Guid unitId) =>
        Call("test unit deleted", "failed to delete test unit", () => _wrapper.Client.DeleteTestUnitAsync(unitId));
}