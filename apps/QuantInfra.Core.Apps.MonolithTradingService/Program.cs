using System.Reflection;
using QuantInfra.Common.Utils.ExecutableAppBase;
using QuantInfra.Services.Api;
using QuantInfra.Services.MonolithService;


var addHostedService = Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider";
        
var host = new AppBase(args)
    .UseJsonFileConfiguration()
    .UseEnvironmentVariables()
    .ConfigureControllers(null, typeof(AccountsController).Assembly)
    .AddOpenApiDocumentGenerator()
    .AddCors()
    .AddJsonOptions()
    .AddLogging()
    .AddMetrics()
    .ConfigureServices((builder, services, configuration) =>
    {
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