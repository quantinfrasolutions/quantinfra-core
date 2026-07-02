using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Backtesting.Abstractions;

namespace QuantInfra.Services.BacktestingCore;

public static class ConfigurationExtensions
{
    // public static IServiceCollection ConfigureBacktestingCore(this IServiceCollection services, IConfiguration configuration, string sectionName = "test-server") => services
    //     .Configure<TestServerConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
    //     .AddSingleton<TestServerConfig>(sp => sp.GetService<IOptions<TestServerConfig>>()?.Value ?? new());
    
    public static IServiceCollection AddBacktestingCore(this IServiceCollection services) => services;
}