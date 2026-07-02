using System.Globalization;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Services.LocalTestServer;

public class ProgressTracker(Guid unitId, ITestUnitsRepository repository) : IActionProgressTracker
{
    public void SetCurrentProgress(double pct)
    {
        repository.SetUnitStatus(unitId, TestUnitStatus.Running, Math.Round(pct * 100, 0).ToString(CultureInfo.InvariantCulture) + "%");
    }

    public void SetTestExecutionTime(long ms) => ExecutionTimeUs = ms;
    
    public long ExecutionTimeUs { get; private set; }
}