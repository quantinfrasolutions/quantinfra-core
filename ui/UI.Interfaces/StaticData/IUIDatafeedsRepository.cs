using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUIDatafeedsRepository
{
    Task<IEnumerable<Datafeed>> GetDatafeeds(PagingFilter filter);
}