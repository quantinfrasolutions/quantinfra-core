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
host.MapGet("/api/debug/routes", (IEnumerable<EndpointDataSource> sources) =>

    sources.SelectMany(s => s.Endpoints)

        .OfType<RouteEndpoint>()

        .Select(e => new

        {

            Route = e.RoutePattern.RawText,

            Methods = e.Metadata

                .OfType<HttpMethodMetadata>()

                .FirstOrDefault()

                ?.HttpMethods,

            DisplayName = e.DisplayName

        }));
host.MapFallbackToFile("index.html");

await QuantInfra.Core.Apps.StrategyTesterCli.Helpers.MigrateSqliteAsync(host.Services);

host.Run();