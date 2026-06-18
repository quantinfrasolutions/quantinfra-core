using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CommissionsFilter : PagingFilter
{
    public int? CommissionId { get; set; }
    public string? Name { get; set; }
    public int? CurrencyId { get; set; }
    public CommissionStructureType? Type { get; set; }
    public int? BrokerId { get; set; }
    public int? ExchangeId { get; set; }
}