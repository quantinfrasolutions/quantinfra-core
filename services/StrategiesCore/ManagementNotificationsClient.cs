// using Common.Infrastructure.Abstractions;
// using Disruptor.Dsl;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using QuantInfra.Domain.Events.Strategies.Management;
// using StrategiesCore;
//
// namespace QuantInfra.Services.StrategiesCore;
//
// public class ManagementNotificationsClient : Client, IManagementNotificationsClient
// {
//     private readonly string _serviceName;
//
//     public ManagementNotificationsClient(Config config, ClientConfig clientConfig, IListenerFactory factory,
//         IClock clock, Disruptor<IncomingDisruptorMessage> inputDisruptor, ILogger<Client> logger
//     ) : base(clientConfig, factory, clock, inputDisruptor, logger)
//     {
//         _serviceName = config.StrategiesServiceName;
//     }
//
//     public override bool CheckMessage(object? msg)
//     {
//         return (msg is StrategyCreatedEvt evt && evt.Strategy.StrategyServiceName == _serviceName);
//     }
// }