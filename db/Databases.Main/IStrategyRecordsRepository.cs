using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Databases.Main;

public partial class MainContext : IStrategyRecordsRepository
{
    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByStrategiesServiceNameAsync(string strategiesServiceName) =>
        await StrategyServiceInstances
            .Include(ss => ss.Strategies)
            .Where(ss => ss.Name == strategiesServiceName)
            .SelectMany(ss => ss.Strategies.Select(s => s))
            .AsNoTracking()
            .ToListAsync();
    
    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByAccountServiceName(string accountsServiceName) =>
        await Strategies
            .Where(s => s.Account.AccountServiceName == accountsServiceName)
            .AsNoTracking()
            .ToListAsync();

    // public Task<IReadOnlyCollection<EsaSubscription>> GetExecutableSubaccountsByStrategiesServiceNameAsync(string strategiesServiceName)
    // {
    //     // TODO;
    //     return Task.FromResult((IReadOnlyCollection<EsaSubscription>)Array.Empty<EsaSubscription>());
    // }

    public async Task<(Strategy, AccountRecordV6)> CreateStrategyAsync(CreateStrategyRequest request, int userId)
    {
        if (string.IsNullOrEmpty(request.Account.Name)) request.Account.Name = $"{request.Account.AccountType} {request.Name}";
        
        var accountModel = CreateAccountInternal(request.Account, userId);
        var model = new StrategyModel(0, request.Name, request.ClassName, request.Params, request.RequiredBarStorages,
            request.Symbols, 
            null, //request.LiquidationParameters, 
            request.UseSignalGroups, 
            request.StartImmediately ? StrategyStatus.Running : StrategyStatus.Stopped, 0, request.StrategyServiceName, 
            accountModel);
        Strategies.Add(model);
        await SaveChangesAsync();

        return (model, accountModel);
    }

    public async Task<Strategy> GetStrategyRecordAsync(int strategyId) =>
        await Strategies.AsNoTracking().SingleAsync(s => s.StrategyId == strategyId);

    public async Task UpdateStrategyStatusAsync(int strategyId, StrategyStatus status)
    {
        var strategy = await Strategies.SingleAsync(s => s.StrategyId == strategyId);
        strategy.Status = status;
        await SaveChangesAsync();
    }
}