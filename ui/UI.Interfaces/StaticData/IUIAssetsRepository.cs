using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiAssetsRepository
{
    public Task<IEnumerable<Asset>> GetAssets(AssetFilter? filter = null);
    public Task CreateAsset(Asset asset);
    public Task DeleteAsset(long id);
}