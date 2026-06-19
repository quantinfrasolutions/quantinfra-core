using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class SharePriceHistoryFilter : PagingFilter
{
    public int AccountId { get; set; }
    public bool SortDescending { get; set; }
    public long? FromDt { get; set; }
    public long? ToDt { get; set; }
    public SharePriceHistoryChangeType? ChangeType { get; set; }
}