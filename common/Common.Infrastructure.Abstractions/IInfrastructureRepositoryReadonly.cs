using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Infrastructure.Abstractions;

namespace QuantInfra.Common.Infrastructure.Abstractions;

public interface IInfrastructureRepositoryReadonly
{
    Task<IReadOnlyCollection<Location>> GetLocationsAsync();
    Task<IReadOnlyCollection<AccountServiceInstance>> GetAccountServiceInstancesAsync();
    Task<IReadOnlyCollection<StrategiesServiceInstance>> GetStrategiesServiceInstancesAsync();
    Task<IReadOnlyCollection<ExecutionServiceInstance>> GetExecutionServiceInstancesAsync();
    Task<IReadOnlyCollection<MarketDataClientInstance>> GetMarketDataClientInstancesAsync();
}

public interface IInfrastructureRepository : IInfrastructureRepositoryReadonly
{
    Task CreateLocationAsync(Location location);
    Task CreateAccountServiceInstanceAsync(AccountServiceInstance instance);
    Task CreateStrategiesServiceInstanceAsync(StrategiesServiceInstance instance);
    Task CreateExecutionServiceInstanceAsync(ExecutionServiceInstance instance);
    Task CreateMarketDataClientInstanceAsync(MarketDataClientInstance instance);
}