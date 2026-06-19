using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Common.Utils.Cryptography;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Databases.Main.DAL;

public class TradingAccountsRepository(IServiceProvider serviceProvider, ISecretProvider secretProvider) : 
    ITradingAccountsRepositoryReadonly,
    ITradingAccountsRepository
{
    public async Task<IReadOnlyCollection<AccountRecordV6>> GetTradingAccountsByExecutionServiceId(string executionServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        var encryptor = new SecretEncryptor(secretProvider.GetOrCreateMasterSecret());
        
        var configs = (
            await context.Accounts
                .Where(a => a.TradingClientConfig != null && a.TradingClientConfig.ExecutionServiceName == executionServiceName)
                .Include(a => a.TradingClientConfig)
                .Include(a => a.Currency)
                    .ThenInclude(c => c.Asset)
                .Include(a => a.Broker)
                .Select(a => (AccountRecordV6)a)
                .AsNoTracking()
                .ToListAsync()
            )
            .Select(a =>
            {
                if (string.IsNullOrEmpty(a.TradingClientConfig?.TradingClientSecret)) return a;
                
                var decrypted = encryptor.DecryptFromBase64(a.TradingClientConfig.TradingClientSecret);
                a = new(a)
                {
                    TradingClientConfig = new(a.TradingClientConfig) { TradingClientSecret = decrypted }
                };
                return a;
            })
            .ToList();

        return configs;
    }

    public async Task CreateTradingClientConfig(TradingClientConfig config)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        if (!string.IsNullOrEmpty(config.TradingClientSecret))
        {
            var encryptor = new SecretEncryptor(secretProvider.GetOrCreateMasterSecret());
            config = new(config) { TradingClientSecret = encryptor.EncryptToBase64(config.TradingClientSecret) };
        }
        
        context.TradingClients.Add(config);
        await context.SaveChangesAsync();
    }

    public async Task RemoveTradingClientConfig(int accountId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        context.TradingClients.Remove(context.TradingClients.Single(c => c.AccountId == accountId));
        await context.SaveChangesAsync();
    }
}