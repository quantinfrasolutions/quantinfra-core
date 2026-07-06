using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateCommissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal FixedPerShare { get; set; }
    public decimal Floating { get; set; }
    public int CurrencyId { get; set; }

    public CommissionStructureType CommissionStructureType { get; set; } = CommissionStructureType.Other;
    public int? BrokerId { get; set; }
    public int? ExchangeId { get; set; }
}