using QuantInfra.Common.Utils.ExecutableAppBase;
using QuantInfra.Connectors.Binance.StaticDataClient;
using QuantInfra.Core.Services.Api.StaticData;
using QuantInfra.Services.Api;
using QuantInfra.Services.Api.Binance;
using QuantInfra.Services.MonolithService;


var host = new AppBase(args)
    .UseJsonFileConfiguration()
    .UseEnvironmentVariables()
    .ConfigureControllers(null, typeof(AccountsController).Assembly, typeof(StaticDataController).Assembly,
        typeof(BinanceController).Assembly)
    .AddCors()
    .AddJsonOptions()
    .AddLogging()
    .AddMetrics()
    .ConfigureServices((builder, services, configuration) =>
    {
        services.AddCachingBinanceStaticDataClient();
        services.ConfigureMonolithService(configuration);
        services.AddMonolithService(configuration);
    })
    // .AddHealthChecks(builder => builder
    //     .AddCheck<HeartbeatsProcessed>("Heartbeats are processed")
    //     .AddCheck<MarketDataProcessed>("Market data events are processed")
    // )
    
    .Build();

host.UseBlazorFrameworkFiles();
host.UseStaticFiles();
host.MapFallbackToFile("index.html");

host.Run();