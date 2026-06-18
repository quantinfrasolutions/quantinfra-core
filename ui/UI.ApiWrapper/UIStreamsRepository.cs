using QuantInfra.Common.Interfaces.Api.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiStreamsRepository
{
    public Task<IEnumerable<StreamListView>> GetStreams(StreamsFilter? filter = null) =>
        RetrieveCollection("streams", () => 
            _wrapper.Client.GetStreamsAsync(filter?.Ticker, filter?.ContractId, filter?.StreamIds, filter?.Limit, filter?.Offset)
        );

    public Task CreateStream(Stream stream)
    {
        throw new NotImplementedException();
    }

    public Task DeleteStream(int id)
    {
        throw new NotImplementedException();
    }
}