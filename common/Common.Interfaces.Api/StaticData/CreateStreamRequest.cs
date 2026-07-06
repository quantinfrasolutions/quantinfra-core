namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateStreamRequest
{
    public string Ticker { get; set; } = string.Empty;
    public int DatafeedId { get; set; }
    public int? ContractId { get; set; }
    public decimal? ConstantValue { get; set; }
    // public string? ConstantValueCron { get; set; } = "";
}
