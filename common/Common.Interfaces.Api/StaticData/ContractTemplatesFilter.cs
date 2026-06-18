namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractTemplatesFilter : PagingFilter
{
    public int? TemplateId { get; set; }
    public string? Name { get; set; }
}