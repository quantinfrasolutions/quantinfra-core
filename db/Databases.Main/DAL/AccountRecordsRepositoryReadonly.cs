using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Databases.Main.DAL;

public class AccountRecordsRepositoryReadonly(IServiceProvider serviceProvider) : IAccountRecordsRepositoryReadonly
{
    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(string accountServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetAccountRecordsAsync(accountServiceName);
    }

    public async Task<AccountRecordV6?> GetAccountRecordAsync(int accountId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetAccountRecordAsync(accountId);
    }

    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(ICollection<int> accountIds)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetAccountRecordsAsync(accountIds);
    }

    public async Task<IReadOnlyCollection<Subaccount>> GetSubaccountsAsync(string accountServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetSubaccountsAsync(accountServiceName);
    }

    public Task<IReadOnlyCollection<AccountRecordV6>> GetBrokerAccountsByExecutionServiceId(string executionServiceName)
    {
        throw new NotImplementedException();
    }
}