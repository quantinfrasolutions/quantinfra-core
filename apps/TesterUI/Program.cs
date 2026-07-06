using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuantInfra.Backtesting.FileResultsRepository;
using QuantInfra.Backtesting.LocalTestServerWrapper;
using QuantInfra.Common.Utils.ExecutableAppBase;
using QuantInfra.Core.Services.Api.StaticData;
using QuantInfra.Databases.Backtesting.Sqlite;
using QuantInfra.Databases.Main;
using QuantInfra.Services.Api.Backtesting;
using QuantInfra.Services.LocalTestServer;

var host = new AppBase(args)
    .UseJsonFileConfiguration()
    .UseEnvironmentVariables()
    .ConfigureControllers(null, typeof(TestsController).Assembly, typeof(StaticDataController).Assembly)
    
    .AddCors()
    .AddJsonOptions()
    .AddLogging()
    .AddMetrics()
    .ConfigureServices((builder, services, configuration) =>
    {
        services
            .Configure<QuantInfra.Core.Apps.TesterUI.Config>(conf => configuration.GetSection("app").Bind(conf))
                
            .ConfigureLocalTestServer(configuration)
            
            .AddSingleton<QuantInfra.Databases.Backtesting.Sqlite.Config>(sp =>
            {
                var config = sp.GetRequiredService<LocalTestServerConfig>();
                return new()
                {
                    DbPath = Path.Join(config.WorkingDirectory, ".db.sqlite"),
                };
            })
            .AddSqliteBacktesting()
            .UseSqliteBacktestingUnitsRepository()
            
            .AddSingleton<QuantInfra.Backtesting.FileResultsRepository.Config>(sp =>
            {
                var config = sp.GetRequiredService<LocalTestServerConfig>();
                return new() { WorkingDirectory = config.WorkingDirectory };
            })
            .AddFileResultsRepository()
            
            .ConfigureMainDb(configuration)
            .AddMainDbContext()
            .UseMainDbStaticDataRepositoryReadOnly()
            
            .ConfigureCliWrapper(configuration)
            .AddCliWrapper();
    })
    
    .Build();

host.UseBlazorFrameworkFiles();
host.UseStaticFiles();
host.MapFallbackToFile("index.html");

await QuantInfra.Core.Apps.StrategyTesterCli.Helpers.MigrateSqliteAsync(host.Services);
if (host.Services.GetService<IOptions<QuantInfra.Core.Apps.TesterUI.Config>>()?.Value?.MigrateMainDb == true)
{
    await Task.Run(async () =>
    {
        await using var scope = host.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Main database migrated");
    });
}

host.Run();