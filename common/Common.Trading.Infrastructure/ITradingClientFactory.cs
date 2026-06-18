using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Infrastructure;

namespace QuantInfra.Common.Trading.Infrastructure;

public interface ITradingClientFactory
{
    IHostedTradingClient GetTradingClient(TradingClientConfig config);
}