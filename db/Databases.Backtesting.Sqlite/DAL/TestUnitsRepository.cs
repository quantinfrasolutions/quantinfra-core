using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Databases.Backtesting.Sqlite.DAL;

public class TestUnitsRepository(IDbContextFactory<BacktestingContext> contextFactory) : ITestUnitsRepository
{
    public async Task<IReadOnlyCollection<TestUnitListView>> GetTestUnitsAsync(TestUnitsFilter filter)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var createdAtFrom = filter.CreatedAtFrom.FromApiFormat();
        var createdAtTo = filter.CreatedAtTo.FromApiFormat();
        return await context.TestUnits
            .Where(u => 
                (!filter.TestId.HasValue || u.TestId == filter.TestId)
                && (string.IsNullOrEmpty(filter.Action) || u.Action == filter.Action)
                && (!createdAtFrom.HasValue || createdAtFrom.Value <= u.CreatedAt)
                && (!createdAtTo.HasValue || createdAtTo.Value >= u.CreatedAt)
            )
            .OrderByDescending(u => u.CreatedAt)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .Select(u => new TestUnitListView(u.TestId, u.Action, u.CreatedAt, u.Status))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TestUnitStatusRecord?> GetTestUnitAsync(Guid unitId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.TestUnits.SingleOrDefaultAsync(t => t.TestId == unitId);
    }

    public async Task CreateTestUnitAsync(TestUnit testUnit)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.TestUnits.Add(new TestUnitStatusRecord(testUnit));
        await context.SaveChangesAsync();
    }

    public async Task DeleteTestUnitAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var unit = await context.TestUnits.SingleOrDefaultAsync(t => t.TestId == id);
        if (unit != null)
        {
            context.TestUnits.Remove(unit);
            await context.SaveChangesAsync();
        }
    }

    public async Task SetUnitStatus(Guid unitId, Sdk.Backtesting.TestUnitStatus status, string? statusMessage = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var unit = await context.TestUnits.SingleAsync(t => t.TestId == unitId);
        unit.Status = status;
        unit.StatusMessage = statusMessage;
        await context.SaveChangesAsync();
    }
}