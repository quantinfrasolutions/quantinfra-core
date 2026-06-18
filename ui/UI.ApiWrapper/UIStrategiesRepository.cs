using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Common.Strategies;
using UI.Interfaces;
using UI.Interfaces.Strategies;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiStrategiesRepository, IUiStrategyClassesRepository
{
    public Task<IEnumerable<StrategyViewBrief>> GetStrategies(StrategiesFilter? filter = null) =>
        RetrieveCollection("strategies",
            () => _wrapper.Client.GetStrategiesAsync(
                filter?.Status, 
                filter?.ClassNames, 
                filter?.StrategyIds,
                filter?.Limit, 
                filter?.Offset
            ));

    public Task CreateStrategy(CreateStrategyRequest strategy) =>
        Call("Strategy created", "Could not create strategy", () => _wrapper.Client.CreateStrategyAsync(strategy));

    public Task StartStrategy(int strategyId)
    {
        throw new NotImplementedException();
    }

    public Task StopStrategy(int strategyId, bool force)
    {
        throw new NotImplementedException();
    }

    public async Task<StrategyViewBrief> GetStrategy(int strategyId) =>
        (await GetStrategies(new StrategiesFilter() { StrategyIds = [strategyId] })).SingleOrDefault();

    public Task<StrategyTypeDescription> GetStrategyClassDescription(string className) =>
        throw new NotImplementedException();
        // Retrieve("strategy description", () => _wrapper.ApiClient.GetStrategyClassAsync(className));
        

    public Task<ValidateStrategyResult> ValidateNewStrategy(CreateStrategyRequest request) =>
        Retrieve("", new EmptyFilter(), _ => _wrapper.Client.ValidateStrategyRequestAsync(request));
    // RetrieveCollection("execution stats", () => _wrapper.ApiClient.GetStrategyExecutionStatsAsync(filter.StrategyId,
        //     filter.AccountId, filter.ContractId,
        //     InstantToString(filter.FromDt), InstantToString(filter.ToDt), filter.GroupByAccount, filter.GroupBySignalGroup, filter.GroupByTimeOfDay,
        //     filter.GroupByDayOfWeek,
        //     filter.GroupByDayStart));

    public Task<IEnumerable<StrategyTypeDescription>> GetAvailableStrategyClasses() =>
        throw new NotImplementedException();
        // RetrieveCollection("strategy classes", () => _wrapper.ApiClient.GetStrategyClassesAsync());
}