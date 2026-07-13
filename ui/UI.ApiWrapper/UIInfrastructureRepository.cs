using Common.Infrastructure.Abstractions;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Interfaces.Api.Infrastructure;
using UI.Interfaces;
using UI.Interfaces.Infrastructure;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiInfrastructureRepository
{
    public Task<IEnumerable<Location>> GetLocations(EmptyFilter? arg = null) =>
        RetrieveCollection("locations", () => _wrapper.Client.GetLocationsAsync());

    public Task<IEnumerable<AccountServiceInstance>> GetAccountServiceInstances(EmptyFilter? arg = null) =>
        RetrieveCollection("account service instances", () => _wrapper.Client.GetAccountServiceInstancesAsync());

    public Task<IEnumerable<StrategiesServiceListView>> GetStrategiesServiceInstances(EmptyFilter? arg = null) =>
        RetrieveCollection("strategy service instances", () => _wrapper.Client.GetStrategiesServiceInstancesAsync());

    public Task<IEnumerable<ExecutionServiceListView>> GetExecutionServiceInstances(EmptyFilter? arg = null) =>
        RetrieveCollection("execution service instances", () => _wrapper.Client.GetExecutionServiceInstancesAsync());

    public Task<IEnumerable<HostedComponentStatus>> GetHostedComponents(EmptyFilter? filter = null) =>
        RetrieveCollection("components", () => _wrapper.Client.GetHostedComponentsAsync());

    public Task StartComponent(string name) =>
        Call("Component startup initiated", "Failed", () => _wrapper.Client.StartComponentAsync(name));

    public Task StopComponent(string name) =>
        Call("Component stop initiated", "Failed", () => _wrapper.Client.StopComponentAsync(name));

    public Task ClearStaticDataCache() =>
        Call("Clear cache command sent", "Failed", () => _wrapper.Client.ClearStaticDataCacheAsync());
}