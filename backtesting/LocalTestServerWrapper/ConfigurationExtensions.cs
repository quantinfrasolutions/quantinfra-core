using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Backtesting.Abstractions;

namespace QuantInfra.Backtesting.LocalTestServerWrapper;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureCliWrapper(this IServiceCollection services, IConfiguration configuration, string sectionName = "cli-wrapper",
        List<string>? copyConfiguration = null, Action<IServiceProvider, Config>? configureAction = null) => services
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp =>
        {
            var conf = sp.GetService<IOptions<Config>>()?.Value ?? new();

            if (copyConfiguration != null)
            {
                conf.Args = configuration
                    .AsEnumerable()
                    .Where(x => x.Value != null && copyConfiguration.Contains(x.Value))
                    .Select(x => $"--{x.Key}={x.Value}")
                    .ToList();
            }
            
            configureAction?.Invoke(sp, conf);
            
            return conf;
        });

    public static IServiceCollection AddCliWrapper(this IServiceCollection services) => services
        .AddSingleton<Wrapper>()
        .AddSingleton<ITestServer>(sp => sp.GetRequiredService<Wrapper>());
}