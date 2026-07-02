using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Databases.Backtesting.Sqlite.DAL;

namespace QuantInfra.Databases.Backtesting.Sqlite;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureSqliteBacktesting(this IServiceCollection services, IConfiguration configuration, string sectionName = "db.sqlite",
        Action<IServiceProvider, Config>? configureAction = null) => services
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<Config>>().Value;
            configureAction?.Invoke(sp, config);
            return config;
        });

    public static IServiceCollection AddSqliteBacktesting(this IServiceCollection services) => services
        .AddDbContextFactory<BacktestingContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<Config>();
            ConfigureOptions(options, config);
        });

    internal static void ConfigureOptions(DbContextOptionsBuilder optionsBuilder, Config config)
    {
        optionsBuilder.UseSqlite($"Data Source={config.DbPath};", o =>
        {
            o.UseNodaTime();
            o.MigrationsHistoryTable("migrations_history");
        });
    }
    
    public static IServiceCollection UseSqliteBacktestingUnitsRepository(this IServiceCollection services) => services
        .AddSingleton<TestUnitsRepository>()
        .AddSingleton<ITestUnitsRepository>(sp => sp.GetRequiredService<TestUnitsRepository>());
}