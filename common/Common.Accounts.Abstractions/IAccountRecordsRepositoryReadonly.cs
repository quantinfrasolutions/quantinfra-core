using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Accounts.Abstractions;

public interface IAccountRecordsRepositoryReadonly
{
    Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(string accountServiceName);
    Task<AccountRecordV6?> GetAccountRecordAsync(int accountId);
    Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(ICollection<int> accountIds);
    Task<IReadOnlyCollection<Subaccount>> GetSubaccountsAsync(string accountServiceName);
}