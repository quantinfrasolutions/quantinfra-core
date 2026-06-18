using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Trading.Infrastructure;

public interface ITradingAccountsRepositoryReadonly
{
    Task<IReadOnlyCollection<AccountRecordV6>> GetTradingAccountsByExecutionServiceId(string executionServiceName);
}