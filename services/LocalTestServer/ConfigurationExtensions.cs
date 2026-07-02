using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QuantInfra.Services.LocalTestServer;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureLocalTestServer(this IServiceCollection services, IConfiguration configuration, string sectionName = "local-test-server",
        Action<IServiceProvider, LocalTestServerConfig>? configureAction = null) => services
        .Configure<LocalTestServerConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<LocalTestServerConfig>(sp =>
        {
            var conf = sp.GetRequiredService<IOptions<LocalTestServerConfig>>().Value;
            configureAction?.Invoke(sp, conf);
            return conf;
        });
    
    public static IServiceCollection AddLocalTestServer(this IServiceCollection services) => services
        .AddSingleton<LocalTestServer>();
}