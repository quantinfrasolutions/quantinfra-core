using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Backtesting.FileResultsRepository;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureFileResultsRepository(this IServiceCollection services, IConfiguration configuration, string sectionName = "fileResults",
        Action<IServiceProvider, Config>? configureAction = null) => services
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp =>
        {
            var conf = sp.GetRequiredService<IOptions<Config>>().Value;
            configureAction?.Invoke(sp, conf);
            return conf;
        });
    
    public static IServiceCollection AddFileResultsRepository(this IServiceCollection services) => services
        .AddSingleton<TestResultsFileReader>()
        .AddSingleton<ITestResultsRepositoryReadonly>(sp => sp.GetRequiredService<TestResultsFileReader>());
    
    public static IServiceCollection AddFileResultsPersister(this IServiceCollection services) => services
        .AddSingleton<TestResultsFileWriter>()
        .AddSingleton<ITestResultsPersister>(sp => sp.GetRequiredService<TestResultsFileWriter>());
}