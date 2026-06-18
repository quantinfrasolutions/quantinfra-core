using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QuantInfra.Common.EventSourcing;

public static class ConfigurationExtensions
{
    public static IServiceCollection UseScopedInMemoryBus(this IServiceCollection sc) => sc
        .AddScoped<InMemoryBus>()
        .AddScoped<IEventBus>(sp => sp.GetService<InMemoryBus>()!)
        .AddScoped<ICommandBus>(sp => sp.GetService<InMemoryBus>()!)
        .AddScoped<IQueryBus>(sp => sp.GetService<InMemoryBus>()!);
    
    public static IServiceCollection UseSingletonInMemoryBus(this IServiceCollection sc) => sc
        .AddSingleton<InMemoryBus>()
        .AddSingleton<IEventBus>(sp => sp.GetService<InMemoryBus>()!)
        .AddSingleton<ICommandBus>(sp => sp.GetService<InMemoryBus>()!)
        .AddSingleton<IQueryBus>(sp => sp.GetService<InMemoryBus>()!);
}