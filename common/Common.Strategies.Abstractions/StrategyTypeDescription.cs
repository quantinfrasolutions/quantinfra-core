namespace QuantInfra.Common.Strategies.Abstractions;

public class StrategyTypeDescription
{
    public string Name { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public Dictionary<string, string> Params { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Version { get; set; } = 1;
    public List<string> Symbols { get; set; } = new List<string>();
    public List<string> RequiredBarStorages { get; set; } = new List<string>();
}