namespace QuantInfra.Common.Interfaces.Api;

public struct BriefView<TKey>
{
    public TKey Id { get; init; } = default;
    public string Name { get; init; } = string.Empty;
    
    public BriefView() { }

    public BriefView(TKey id, string name)
    {
        Id = id;
        Name = name;
    }

    // public static BriefView<long> Create(ContractDefinition c) => new(c.ContractId, c.Ticker);
    //
    // public static BriefView<long> Create(ContractModel c) => new(c.ContractId, c.Ticker);
    //
    // public static BriefView<long> Create(StreamDefinition s, string? currencyName) => new(
    //     s.StreamId, 
    //     string.IsNullOrEmpty(currencyName)
    //         ? s.Ticker
    //     : $"{s.Ticker}/{currencyName}"
    // );
    //
    // public static BriefView<long> Create(Currency c) => new(c.Id, c.Name);
}