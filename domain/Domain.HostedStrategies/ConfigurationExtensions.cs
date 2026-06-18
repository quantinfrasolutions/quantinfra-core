using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.HostedStrategies;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureHostedStrategies(this IServiceCollection sc, IConfiguration configuration, string sectionName = "strategies-runner") => sc
        .Configure<HostedStrategiesRunnerConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<HostedStrategiesRunnerConfig>(sp => sp.GetRequiredService<IOptions<HostedStrategiesRunnerConfig>>().Value);

    public static IServiceCollection AddHostedStrategiesRunner(this IServiceCollection sc) => sc
        .AddSingleton<HostedStrategiesRunner>()
        .AddSingleton<IEventHandler<ExecutionReportEvt>>(sp => sp.GetRequiredService<HostedStrategiesRunner>())
        .AddSingleton<IEventHandler<Candle1MClosedEvt>>(sp => sp.GetRequiredService<HostedStrategiesRunner>())
        .AddSingleton<IEventHandler<AggregatedOrderbookUpdateEvt>>(sp => sp.GetRequiredService<HostedStrategiesRunner>())
        .AddSingleton<IEventHandler<BestBidAskUpdatedEvt>>(sp => sp.GetRequiredService<HostedStrategiesRunner>())
        .AddSingleton<IAsyncQueryResponseHandler<GetOrderBookSnapshot, OrderBookSnapshot?>>(sp => sp.GetRequiredService<HostedStrategiesRunner>());
}