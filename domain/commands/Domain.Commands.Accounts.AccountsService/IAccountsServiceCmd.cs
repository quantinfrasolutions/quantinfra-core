using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public interface IAccountsServiceCmd : ICommand
{
    string AccountServiceName { get; }
}