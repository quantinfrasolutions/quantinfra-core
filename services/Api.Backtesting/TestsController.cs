using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Services.Api.Backtesting;

[ApiController]
[Route("api/tests")]
public class TestsController(ITestUnitsRepository repository)
{
    [HttpGet]
    [EndpointName(nameof(GetTestUnits))]
    [Produces("application/json")]
    public async Task<IEnumerable<TestUnitListView>> GetTestUnits([FromQuery] TestUnitsFilter? filter = null)
    {
        return await repository.GetTestUnitsAsync(filter ?? new());
    }

    [HttpPost]
    [EndpointName(nameof(CreateTestUnit))]
    public Task CreateTestUnit([FromBody] TestUnit testUnit)
    {
        return repository.CreateTestUnitAsync(testUnit);
    }

    [HttpGet("{id:guid}")]
    [EndpointName(nameof(GetTestUnit))]
    [Produces("application/json")]
    public Task<TestUnitStatusRecord?> GetTestUnit(Guid id)
    {
        return repository.GetTestUnitAsync(id);
    }

    [HttpDelete("{id:guid}")]
    [EndpointName(nameof(DeleteTestUnit))]
    public Task DeleteTestUnit([FromRoute] Guid id)
    {
        return repository.DeleteTestUnitAsync(id);
    }
}