namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class StreamsFilter : PagingFilter
{
    public string? Ticker { get; set; }
    public int? ContractId { get; set; }
    public List<int>? StreamIds { get; set; } = null;
}