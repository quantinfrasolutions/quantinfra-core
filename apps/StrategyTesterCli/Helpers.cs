using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Databases.Backtesting.Sqlite;

namespace QuantInfra.Core.Apps.StrategyTesterCli;

public static class Helpers
{
    public static async Task MigrateSqliteAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BacktestingContext>>();

        await using var db = await factory.CreateDbContextAsync();

        await db.Database.MigrateAsync();

        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
    }
}