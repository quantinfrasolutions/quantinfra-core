using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData;

public interface IStaticDataRepositoryStateStore
{
    Dictionary<int, Contract?> Contracts { get; }
    Dictionary<int, Dictionary<string, Contract?>> ContractsByExternalId { get; }
    Dictionary<int, Currency?> Currencies { get; }
    Dictionary<int, Dictionary<int, IReadOnlyCollection<FxConversionStep>>> ConversionPaths { get; }
    Dictionary<int, Broker?> Brokers { get; }
    Dictionary<int, Dictionary<string, Asset?>> AssetsByExternalId { get; }
}