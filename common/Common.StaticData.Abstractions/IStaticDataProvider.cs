using QuantInfra.Sdk.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace Common.StaticData.Abstractions;

public interface IStaticDataProvider
{
    Currency? GetCurrency(int id);
    Contract? GetContract(int contractId);
    Contract? GetContractByExternalId(int brokerId, string externalContractId);
    IReadOnlyCollection<int> GetFxConversionContractIds();
    Asset? GetAssetByExternalId(int brokerId, string externalAssetId);
    IReadOnlyCollection<Contract> GetContracts(IEnumerable<int> contractIds);
    (int contractId, bool isDirect) GetFxConversionContract(int fromCcyId, int toCcyId);
    Broker? GetBroker(int brokerId);
    string? GetContractOrderBookSubscriptionServiceName(int contractId);
    IReadOnlyCollection<Stream> GetStreams(IEnumerable<int> streamIds);
}