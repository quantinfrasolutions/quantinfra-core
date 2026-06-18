// using Disruptor.Dsl;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using NodaTime;
// using QuanInfra.Common.ServiceBase;
// using QuanInfra.Common.ServiceBase.Finalizer;
// using QuantInfra.Common.Messaging;
//
// namespace QuantInfra.Common.ServiceBase.Finalizer;
//
// public static class ConfigurationExtensions
// {
//     public static IServiceCollection ConfigureFinalizer(this IServiceCollection sc, IConfiguration configuration, string sectionName = "finalizer") => sc
//         .Configure<FinalizerConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
//         .AddSingleton<FinalizerConfig>(sp => sp.GetRequiredService<IOptions<FinalizerConfig>>().Value);
//     
//     public static IServiceCollection AddFinalizer(this IServiceCollection sc, string? messageFactoryKey = null)
//     {
//         if (string.IsNullOrEmpty(messageFactoryKey))
//             return sc.AddSingleton<QuanInfra.Common.ServiceBase.Finalizer.Finalizer>();
//
//         return sc.AddSingleton<QuanInfra.Common.ServiceBase.Finalizer.Finalizer>(sp => new(
//             sp.GetRequiredService<FinalizerConfig>(),
//             sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>(),
//             sp.GetRequiredKeyedService<IMessageFactory>(messageFactoryKey),
//             sp.GetRequiredService<IClock>(),
//             sp.GetRequiredService<ILogger<QuanInfra.Common.ServiceBase.Finalizer.Finalizer>>()
//         ));
//     }
// }