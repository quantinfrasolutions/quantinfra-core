using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Accounts.Abstractions;

public interface IAccountRecordsRepository : IAccountRecordsRepositoryReadonly
{
    Task<AccountRecordV6> CreateAccountAsync(CreateAccountRequest account, int userId);
    Task<Subaccount> CreateSubaccountAsync(Subaccount subaccount, int userId);
}