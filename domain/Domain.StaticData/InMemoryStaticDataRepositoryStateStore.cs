using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData;

public class InMemoryStaticDataRepositoryStateStore : IStaticDataRepositoryStateStore
{
    public Dictionary<int, Contract?> Contracts { get; } = new();
    public Dictionary<int, Dictionary<string, Contract?>> ContractsByExternalId { get; } = new();
    public Dictionary<int, Currency?> Currencies { get; } = new();
    public Dictionary<int, Dictionary<int, IReadOnlyCollection<FxConversionStep>>> ConversionPaths { get; } = new();
    public Dictionary<int, Broker?> Brokers { get; } = new();
    public Dictionary<int, Dictionary<string, Asset?>> AssetsByExternalId { get; } = new();
}