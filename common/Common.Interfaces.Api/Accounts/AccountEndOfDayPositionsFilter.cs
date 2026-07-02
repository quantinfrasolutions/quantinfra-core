namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class AccountEndOfDayPositionsFilter
{
    public int AccountId { get; set; }
    public long Dt { get; set; }
}