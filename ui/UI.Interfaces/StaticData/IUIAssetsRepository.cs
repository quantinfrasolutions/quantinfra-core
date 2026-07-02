using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiAssetsRepository
{
    public Task<IEnumerable<Asset>> GetAssets(AssetFilter? filter = null);
    public Task CreateAsset(Asset asset);
    public Task DeleteAsset(long id);
}