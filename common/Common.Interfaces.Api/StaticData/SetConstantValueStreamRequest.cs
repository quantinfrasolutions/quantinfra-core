namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class SetConstantValueStreamRequest
{
    public int StreamId { get; set; }
    public decimal Value { get; set; }
}