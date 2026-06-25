using NodaTime;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.Strategies;

public class ConfigurationChangedException : Exception
{
    public ConfigurationChangedException(string message, Instant referenceDt, HashSet<long> synthContractsToReload,
        IReadOnlyDictionary<long, CompositionUpdate> updatedSynths) : base(message)
    {
        ReferenceDt = referenceDt;
        SynthContractsToReload = synthContractsToReload;
        UpdatedSynths = updatedSynths;
    }
    
    public Instant ReferenceDt { get; }
    public HashSet<long> SynthContractsToReload { get; }
    public IReadOnlyDictionary<long, CompositionUpdate> UpdatedSynths { get; }
}