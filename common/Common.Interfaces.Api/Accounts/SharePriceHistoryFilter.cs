using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class SharePriceHistoryFilter : PagingFilter
{
    public int AccountId { get; set; }
    public bool SortDescending { get; set; }
    public Instant? FromDt { get; set; }
    public Instant? ToDt { get; set; }
    public SharePriceHistoryChangeType? ChangeType { get; set; }
}