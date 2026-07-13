using Common.Infrastructure.Abstractions;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Interfaces.Api.Infrastructure;

namespace UI.Interfaces.Infrastructure;

public interface IUiInfrastructureRepository
{
    Task<IEnumerable<Location>> GetLocations(EmptyFilter? arg = null);
    Task<IEnumerable<AccountServiceInstance>> GetAccountServiceInstances(EmptyFilter? arg = null);
    Task<IEnumerable<StrategiesServiceListView>> GetStrategiesServiceInstances(EmptyFilter? arg = null);
    Task<IEnumerable<ExecutionServiceListView>> GetExecutionServiceInstances(EmptyFilter? arg = null);
    Task<IEnumerable<HostedComponentStatus>> GetHostedComponents(EmptyFilter? filter = null);
    Task StartComponent(string name);
    Task StopComponent(string name);
    Task ClearStaticDataCache();
}