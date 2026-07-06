using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class AssetView : Asset
{
    public AssetView() { }

    public AssetView(Asset asset, int? decimals, bool decimalOverridesExist, IReadOnlyCollection<string> nameOverrides)
        : base(asset)
    {
        Decimals = decimals;
        DecimalOverridesExist = decimalOverridesExist;
        NameOverrides = nameOverrides;
    }
    
    public int? Decimals { get; init; }
    public bool DecimalOverridesExist { get; init; }
    public IReadOnlyCollection<string> NameOverrides { get; init; }
}