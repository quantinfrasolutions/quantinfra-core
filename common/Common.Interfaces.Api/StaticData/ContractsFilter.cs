namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractsFilter : PagingFilter
{
    public virtual string? Ticker { get; set; }
    public virtual long? ExchangeId { get; set; } = null;
    public virtual List<long>? ContractIds { get; set; } = null;
    public virtual long? CommissionId { get; set; }
}