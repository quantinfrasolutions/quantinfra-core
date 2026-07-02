using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class AssetFilter : PagingFilter
{
    public int? Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public AssetType? AssetType { get; set; }
}