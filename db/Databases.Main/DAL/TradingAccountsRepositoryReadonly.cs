using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Databases.Main.DAL;

public class TradingAccountsRepositoryReadonly(IServiceProvider serviceProvider) : ITradingAccountsRepositoryReadonly
{
    public async Task<IReadOnlyCollection<AccountRecordV6>> GetTradingAccountsByExecutionServiceId(
        string executionServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        return await context.Accounts
            .Where(a => a.TradingClientConfig != null && a.TradingClientConfig.ExecutionServiceName == executionServiceName)
            .Include(a => a.TradingClientConfig)
            .Include(a => a.Currency)
                .ThenInclude(c => c.Asset)
            .Include(a => a.Broker)
            .AsNoTracking()
            .ToListAsync();
    }
}