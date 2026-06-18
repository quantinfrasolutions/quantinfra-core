using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Services.ExecutionCore;

public class ExecutionServiceState :
    IQueryHandler<GetAccount, AccountRecordV6?>
{
    public Dictionary<int, AccountRecordV6> Accounts { get; } = new();
    
    public AccountRecordV6? Handle(GetAccount query) => Accounts.GetValueOrDefault(query.AccountId);
}