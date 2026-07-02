using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class EndOfDayValuesFilter : PagingFilter
{
    public int AccountId { get; set; }
    public Instant? From { get; set; }
    public Instant? To { get; set; }
}