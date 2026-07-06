namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractsFilter : PagingFilter
{
    public virtual string? Ticker { get; set; }
    public virtual int? ExchangeId { get; set; } = null;
    public virtual List<int>? ContractIds { get; set; } = null;
    public virtual int? CommissionId { get; set; }
}