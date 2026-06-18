using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetAccount(int AccountId) : 
    IQuery<IAccount?>, 
    IQuery<IAccountStateReadonly?>, 
    IQuery<IBrokerAccountStateReadonly?>,
    // IQuery<IExecutableSubaccount?>,
    IQuery<ITradingAccount?>,
    IQuery<IBrokerAccount?>,
    IQuery<AccountRecordV6?>;