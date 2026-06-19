using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Trading.Infrastructure;

public interface ITradingAccountsRepository : ITradingAccountsRepositoryReadonly
{
    Task CreateTradingClientConfig(TradingClientConfig config);
    Task RemoveTradingClientConfig(int accountId);
}