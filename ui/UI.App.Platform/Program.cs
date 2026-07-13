using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QuantInfra.Api.Client;
using QuantInfra.Api.Client.Binance;
using QuantInfra.UI.ApiWrapper.Backtesting;
using Radzen;
using UI.ApiWrapper;
using UI.App.Platform;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddLogging();
builder.Services.AddSingleton<ILogger>(sp => sp.GetService<ILoggerFactory>()!.CreateLogger("Logger"));
builder.Services.AddRadzenComponents();
builder.Services
    .ConfigureApiServiceWrapper(builder.Configuration, replaceBaseUri: builder.HostEnvironment.BaseAddress)
    .AddScopedApiWrapper()
    .AddApiRepository()
    .UseApiStaticDataRepository()
    .UseApiAccountsRepository()
    .UseApiStrategiesRepository()
    .UseApiInfrastructureRepository()
    
    .ConfigureBinanceApiServiceWrapper(builder.Configuration, replaceBaseUri: builder.HostEnvironment.BaseAddress)
    .AddScopedBinanceApiWrapper()
    .AddBinanceApiClient();

builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "ControlPanelTheme"; // The name of the cookie
    options.Duration = TimeSpan.FromDays(365); // The duration of the cookie
});

await builder.Build().RunAsync();
