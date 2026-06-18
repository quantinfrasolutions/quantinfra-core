using NodaTime;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateSyntheticContractCompositionRequest
{
    public long ContractId { get; set; }
    public List<ContractCompositionModel> CompositionHistory { get; set; } = new();
}

public class ContractCompositionModel
{
    public Instant? ValidFrom { get; set; }
    public Dictionary<int, WeightModel> Weights { get; set; } = new();
    public double? InitialPrice { get; set; }
    
    public SyntheticContractComposition ToComposition() => new()
    {
        ValidFrom = ValidFrom,
        InitialPrice = InitialPrice,
        Weights = Weights.ToDictionary(kv => kv.Key, kv => kv.Value.Weight),
        InitialPrices = Weights.Where(kv => kv.Value.InitialPrice.HasValue)
            .ToDictionary(kv => kv.Key, kv => kv.Value.InitialPrice!.Value)
    };
}

public class WeightModel
{
    public decimal Weight { get; set; }
    public double? InitialPrice { get; set; }
}