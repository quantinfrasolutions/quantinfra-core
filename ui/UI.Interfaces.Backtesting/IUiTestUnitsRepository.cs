using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.UI.Interfaces.Backtesting;

public interface IUiTestUnitsRepository
{
    Task<IEnumerable<TestUnitListView>> GetTestsHistory(TestUnitsFilter? filter = null);
    Task CreateTestUnit(TestUnit unit);
    Task<TestUnitStatusRecord> GetTestUnitStatus(Guid unitId);
    Task DeleteTestUnit(Guid unitId);
}