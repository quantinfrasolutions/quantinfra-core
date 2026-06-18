using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QuantInfra.Api
{
    public static class ConfigurationExtensions
	{
        public static IServiceCollection ConfigureApiServiceWrapper(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "service-wrapper"
        ) => services
            .Configure<ServiceWrapperConfig>(conf =>
                configuration.GetSection(sectionName).Bind(conf)
            )
            .AddSingleton<ServiceWrapperConfig>(sp => sp.GetService<IOptions<ServiceWrapperConfig>>()!.Value);
        
        public static IServiceCollection AddSingletonApiWrapper(
            this IServiceCollection serviceCollection
        ) => serviceCollection.AddSingleton<ServiceWrapper>();
        
        public static IServiceCollection AddScopedApiWrapper(
            this IServiceCollection serviceCollection
        ) => serviceCollection.AddScoped<ServiceWrapper>();
    }
}

