namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class AccountEndOfDayBalancesFilter : PagingFilter
{
    public int AccountId { get; set; }
    public long? FromDt { get; set; }
    public long? ToDt { get; set; }
}