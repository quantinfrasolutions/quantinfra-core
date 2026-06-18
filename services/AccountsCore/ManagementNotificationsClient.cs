// TODO: move to a separate project with concrete implementation
// using AccountsCore;
// using Common.Infrastructure.Abstractions;
// using Disruptor.Dsl;
// using Domain.Queries.Accounts.AccountsService;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using QuantInfra.Domain.Commands.StaticData;
// using QuantInfra.Domain.Events.Accounts.Management;
// using QuantInfra.Domain.Events.Strategies.Management;
//
// namespace QuantInfra.Services.AccountsCore;
//
// public class ManagementNotificationsClient : Client, IManagementNotificationsClient
// {
//     private readonly string _serviceName;
//
//     public ManagementNotificationsClient(Config config, ClientConfig clientConfig, IListenerFactory factory,
//         IClock clock, Disruptor<IncomingDisruptorMessage> inputDisruptor, ILogger<Client> logger
//     ) : base(clientConfig, factory, clock, inputDisruptor, logger)
//     {
//         _serviceName = config.AccountServiceName;
//     }
//
//     public override bool CheckMessage(object? msg)
//     {
//         return (msg is AccountCreatedEvt acEvt && acEvt.Account.AccountServiceName == _serviceName)
//             || (msg is IAccountsServiceCmd cmd && cmd.AccountServiceName == _serviceName)
//             || (msg is IAccountServiceAsyncQuery q && q.AccountServiceName == _serviceName)
//             || (msg is StrategyCreatedEvt scEvt && scEvt.Account.AccountServiceName == _serviceName)
//             || (msg is SubaccountAssignedEvt sa && sa.AccountServiceName == _serviceName)
//             || (msg is ClearStaticDataCacheCmd csd && csd.AccountServiceName == _serviceName);
//     }
// }