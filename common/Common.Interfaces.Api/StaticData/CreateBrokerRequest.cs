using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateBrokerRequest
{
    public string Name { get; set; } = string.Empty;
    public BrokerType BrokerType { get; set; }
}
