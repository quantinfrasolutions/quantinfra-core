using QuantInfra.Common.Interfaces.Api.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiStreamsRepository
{
    public Task<IEnumerable<StreamListView>> GetStreams(StreamsFilter? filter = null);
    // public Task CreateStream(StreamDefinition stream);
    public Task DeleteStream(int id);
}