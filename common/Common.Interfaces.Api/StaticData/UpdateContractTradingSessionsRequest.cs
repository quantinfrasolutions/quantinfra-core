namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class UpdateContractTradingSessionsRequest
{
    public long[] Add { get; set; }
    public long[] Remove { get; set; }
}