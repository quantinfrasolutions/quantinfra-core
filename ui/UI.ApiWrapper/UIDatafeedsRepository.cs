using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUIDatafeedsRepository
{
    public Task<IEnumerable<Datafeed>> GetDatafeeds(PagingFilter filter) =>
        RetrieveCollection("datafeeds", () => _wrapper.Client.GetDatafeedsAsync());
}