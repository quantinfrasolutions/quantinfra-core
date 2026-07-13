using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractTemplatesFilter : PagingFilter
{
    public int? TemplateId { get; set; }
    public string? Name { get; set; }
    public int? BrokerId { get; set; }
    public int? AssetId { get; set; }
    public int? SettlementCurrencyId { get; set; }
    public SecurityType? SecurityType { get; set; }
}
