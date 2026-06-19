using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Tests.Mocks;

public class MockAccountRecordsRepositoryReadonly : IAccountRecordsRepositoryReadonly
{
    public List<AccountRecordV6> Accounts { get; set; } = new();
    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(string accountServiceName) =>
        await Task.Run(() => Accounts.Where(a => a.AccountServiceName == accountServiceName).ToList());

    public Task<AccountRecordV6?> GetAccountRecordAsync(int accountId) =>
        Task.Run(() => Accounts.Single(a => a.AccountId == accountId));

    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(ICollection<int> accountIds) =>
        await Task.Run(() => Accounts.Where(a => accountIds.Contains(a.AccountId)).ToList());

    public async Task<IReadOnlyCollection<Subaccount>> GetSubaccountsAsync(string accountServiceName)
    {
        return new List<Subaccount>();
    }

    public Task<IReadOnlyCollection<AccountRecordV6>> GetBrokerAccountsByExecutionServiceId(string executionServiceName)
    {
        throw new NotImplementedException();
    }
}