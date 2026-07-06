using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateCurrencyRequest
{
    public int? AssetId { get; set; }
    public int Decimals { get; set; } = 2;

    public Currency ToCurrency() => new() { CurrencyId = AssetId!.Value, Decimals = Decimals };
}
