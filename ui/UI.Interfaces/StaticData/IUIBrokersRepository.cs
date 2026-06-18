using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiBrokersRepository
{
    Task<IEnumerable<Broker>> GetBrokers(BrokersFilter? filter = null);
}