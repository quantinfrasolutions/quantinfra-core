using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Backtesting;

public class TestUnitsFilter : PagingFilter
{
    public Guid? TestId { get; set; }
    public long? CreatedAtFrom { get; set; }
    public long? CreatedAtTo { get; set; }
    public string? Action { get; set; }
}