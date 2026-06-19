using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Databases.Main;

public partial class MainContext : IAccountRecordsRepository
{
    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(string accountServiceName) =>
        await GetAccountsQuery(Accounts.Where(a => a.AccountServiceName == accountServiceName))
            .ToListAsync();

    public async Task<AccountRecordV6?> GetAccountRecordAsync(int accountId) =>
        await GetAccountsQuery(Accounts.Where(a => a.AccountId == accountId))
            .SingleOrDefaultAsync();

    public async Task<IReadOnlyCollection<AccountRecordV6>> GetAccountRecordsAsync(ICollection<int> accountIds) =>
        await GetAccountsQuery(Accounts.Where(a => accountIds.Contains(a.AccountId)))
            .ToListAsync();

    public async Task<IReadOnlyCollection<Subaccount>> GetSubaccountsAsync(string accountServiceName) =>
        await Subaccounts.Where(sa => sa.Account.AccountServiceName == accountServiceName)
            .AsNoTracking().ToListAsync();

    public async Task<AccountRecordV6> CreateAccountAsync(CreateAccountRequest request, int userId)
    {
        var model = CreateAccountInternal(request, userId);
        await SaveChangesAsync();
        return model;
    }

    public async Task<Subaccount> CreateSubaccountAsync(Subaccount subaccount, int userId)
    {
        var model = new SubaccountModel(subaccount);
        Subaccounts.Add(model);
        await SaveChangesAsync();
        return model;
    }

    private IQueryable<AccountModel> GetAccountsQuery(IQueryable<AccountModel> source) => source
        .Include(a => a.Currency)
        .ThenInclude(c => c.Asset)
        .Include(a => a.TradingClientConfig)
        .Include(a => a.Broker)
        .AsNoTracking();

    private AccountModel CreateAccountInternal(CreateAccountRequest request, int userId)
    {
        var account = new AccountModel(request.AccountServiceName, request.Name, request.CurrencyId, request.AccountType,
            request.PositionAccounting, request.BrokerId, request.EnableSharePriceTracking, request.IncludeUnrealizedPnLToMtm,
            null, null); // TODO: trading client config, broker
        Accounts.Add(account);
        return account;
    }
}