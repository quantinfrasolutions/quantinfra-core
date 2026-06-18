using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.StaticData.Abstractions;

public class AssetFilter : PagingFilter
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public AssetType? AssetType { get; set; }
}