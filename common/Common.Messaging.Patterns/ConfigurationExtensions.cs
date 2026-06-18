using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;

namespace QuantInfra.Common.Messaging.Patterns;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddDisruptorPublishingSubscriber(this IServiceCollection services) => services
        .AddSingleton<DisruptorPublishingSubscriber>()
        .AddSingleton<IMulticastListenerEventHandler>(sp => sp.GetRequiredService<DisruptorPublishingSubscriber>());
}