using Common.Infrastructure.Abstractions;
using Common.Infrastructure.Abstractions.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/infrastructure")]
public class InfrastructureController(MainContext context)
{
    [HttpGet, Route("locations")]
    [EndpointName(nameof(GetLocations))]
    [Produces("application/json")]
    public async Task<IEnumerable<Location>> GetLocations() => await context.Locations.AsNoTracking().ToListAsync();
    
    [HttpGet, Route("as")]
    [EndpointName(nameof(GetAccountServiceInstances))]
    [Produces("application/json")]
    public async Task<IEnumerable<AccountServiceInstance>> GetAccountServiceInstances() => 
        await context.AccountServiceInstances.AsNoTracking().ToListAsync();
    
    [HttpGet, Route("ss")]
    [EndpointName(nameof(GetStrategiesServiceInstances))]
    [Produces("application/json")]
    public async Task<IEnumerable<StrategiesServiceListView>> GetStrategiesServiceInstances() => 
        await context
            .StrategyServiceInstances
            .Select(ss => new StrategiesServiceListView
            {
                Name = ss.Name, 
                ActiveStrategiesCount = ss.Strategies.Count(s => s.Status == StrategyStatus.Running),
                LocationName = ss.LocationName,
            })
            .AsNoTracking().ToListAsync();
    
    [HttpGet, Route("es")]
    [EndpointName(nameof(GetExecutionServiceInstances))]
    [Produces("application/json")]
    public async Task<IEnumerable<ExecutionServiceListView>> GetExecutionServiceInstances() => 
        await context
            .ExecutionServiceInstances
            .Select(es => new ExecutionServiceListView
            {
                Name = es.Name, 
                TradingClientsCount = es.TradingClients.Count,
                LocationName = es.LocationName,
            })
            .AsNoTracking().ToListAsync();
}