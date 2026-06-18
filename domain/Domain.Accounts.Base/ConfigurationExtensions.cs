using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.CommandHandlers;
using QuantInfra.Domain.Commands.Accounts.AccountsService;

namespace QuantInfra.Domain.Accounts.Base;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddBaseAccounts(this IServiceCollection sc) => sc
        .AddSingleton<ICommandHandler<ProcessBalanceOperationCmd>, ProcessBalanceOperationCmdHandler>()
        .AddSingleton<ICommandHandler<RunEndOfDayCmd>, EndOfDay>();
}