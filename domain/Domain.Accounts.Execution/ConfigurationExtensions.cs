using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

namespace QuantInfra.Domain.Accounts.Execution;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddExecutionAccounts(this IServiceCollection services) => services
        .AddSingleton<IEventHandler<ExecutionReportEvt>, SendActivatedOrderToBrokerAccount>();
}