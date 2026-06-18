using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiAssetsRepository
{
    public Task<IEnumerable<Asset>> GetAssets(AssetFilter? filter = null) =>
        RetrieveCollection("assets", () => _wrapper.Client.GetAssetsAsync(filter?.Id, filter?.Name, filter?.AssetType?.ToString(), filter?.Limit, filter?.Offset));

    public Task CreateAsset(Asset asset)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsset(long id)
    {
        throw new NotImplementedException();
    }
}