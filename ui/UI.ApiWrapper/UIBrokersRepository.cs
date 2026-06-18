using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiBrokersRepository
{
    public Task<IEnumerable<Broker>> GetBrokers(BrokersFilter? filter = null) =>
        RetrieveCollection("brokers", () => _wrapper.Client.GetBrokersAsync(filter?.Limit, filter?.Offset));
}