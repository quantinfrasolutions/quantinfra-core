using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Services.ExecutionCore.Queries;
using QuantInfra.Sdk.Trading.Infrastructure;

namespace QuantInfra.Services.ExecutionCore;

public class HostedTradingClientsProvider(ITradingClientFactory factory)
    : IQueryHandler<GetTradingClient, IHostedTradingClient?>
{
    private Dictionary<int, IHostedTradingClient> _tradingClients = new();

    public async Task StartAsync(IReadOnlyCollection<TradingClientConfig> configs, CancellationToken cancellationToken)
    {
        _tradingClients = configs.ToDictionary(
            c => c.AccountId, 
            factory.GetTradingClient
        );
        
        var delay = Task.Run(async () => await Task.Delay(10000, cancellationToken), cancellationToken);
        if (await Task.WhenAny(
                delay,
                Task.WhenAll(_tradingClients.Values
                    .Select(c => c!.StartAsync(cancellationToken)
                ))
            ) == delay
        )
        {
            throw new TimeoutException("Timeout while starting trading clients");
        }
    }

    public IHostedTradingClient? Handle(GetTradingClient query) => _tradingClients.GetValueOrDefault(query.AccountId);
}