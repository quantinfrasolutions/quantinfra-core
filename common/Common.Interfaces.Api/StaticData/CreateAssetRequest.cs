using System.ComponentModel.DataAnnotations;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateAssetRequest
{
    [Required(ErrorMessage = "Asset name is required")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Asset type is required")] public AssetType AssetType { get; set; } = AssetType.Stock;
    public int Decimals { get; set; } = 2;
    public string? Description { get; set; }

    public Asset ToAsset() => new()
    {
        Name = Name,
        AssetType = AssetType,
        Description = Description,
    };
}
