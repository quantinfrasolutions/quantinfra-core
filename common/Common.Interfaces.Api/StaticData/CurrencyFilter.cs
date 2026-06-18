namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CurrencyFilter : PagingFilter
{
    public long? Id { get; set; }
    public string? Name { get; set; }
}