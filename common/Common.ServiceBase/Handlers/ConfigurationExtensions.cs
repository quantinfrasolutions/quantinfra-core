using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Common.ServiceBase.Handlers;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddEventsForwarder(this IServiceCollection sc) => sc
        .AddSingleton<IEventHandler, ForwardToOutputDisruptorEventHandler>()
        .AddSingleton<IProjectionWriter, ForwardToOutputDisruptorEventHandler>();
}