using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Interfaces.Api.Infrastructure;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/infrastructure")]
public class InfrastructureController(
    MainContext context, 
    IHostedComponentsStatusProvider statusProvider,
    IManagementServiceClient mgmtSvc
) : Controller
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

    [HttpGet, Route("components")]
    [EndpointName(nameof(GetHostedComponents))]
    [Produces("application/json")]
    public async Task<IEnumerable<HostedComponentStatus>> GetHostedComponents()
    {
        return await statusProvider.GetHostedComponentsAsync();
    }

    [HttpPost, Route("components/{name}")]
    [EndpointName(nameof(StartComponent))]
    public async Task<IActionResult> StartComponent(string name)
    {
        try
        {
            await statusProvider.StartComponent(name);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception e)
        {
            if (e.Message == "Already running") return BadRequest("Component already running");
            throw;
        }
    }
    
    [HttpDelete, Route("components/{name}")]
    [EndpointName(nameof(StopComponent))]
    public async Task<IActionResult> StopComponent(string name)
    {
        try
        {
            await statusProvider.StopComponent(name);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception e)
        {
            if (e.Message == "Not running") return BadRequest("Component is not running");
            throw;
        }
    }

    [HttpPost, Route("clear-sd-cache")]
    [EndpointName(nameof(ClearStaticDataCache))]
    public async Task<IActionResult> ClearStaticDataCache()
    {
        var accountServices = (await GetAccountServiceInstances()).ToList();
        await Task.WhenAll(accountServices.Select(accSvc => mgmtSvc.ClearStaticDataCache(accSvc.Name)));
        return Ok();
    }
}