using NodaTime;

namespace QuantInfra.Backtesting.FileResultsRepository;

internal static class FilterHelpers
{
    public static bool MatchesCommonFields<TItem, TFilter>(TItem item, TFilter filter)
    {
        if (filter is null)
            return true;

        return MatchesEquality(item, filter, "AccountId")
               && MatchesEquality(item, filter, "StrategyId")
               && MatchesEquality(item, filter, "ContractId")
               && MatchesFromTo(item, filter, "Date")
               && MatchesFromTo(item, filter, "Timestamp")
               && MatchesFromTo(item, filter, "CreatedAt");
    }

    private static bool MatchesEquality<TItem, TFilter>(
        TItem item,
        TFilter filter,
        string propertyName)
    {
        var filterProp = typeof(TFilter).GetProperty(propertyName);
        if (filterProp is null)
            return true;

        var expected = filterProp.GetValue(filter);
        if (expected is null)
            return true;

        var itemProp = typeof(TItem).GetProperty(propertyName);
        if (itemProp is null)
            return true;

        var actual = itemProp.GetValue(item);
        return Equals(actual, expected);
    }

    private static bool MatchesFromTo<TItem, TFilter>(
        TItem item,
        TFilter filter,
        string itemPropertyName)
    {
        var itemProp = typeof(TItem).GetProperty(itemPropertyName);
        if (itemProp is null)
            return true;

        var actual = itemProp.GetValue(item);
        if (actual is null)
            return true;

        var from = GetFilterValue(filter, "From") 
                   ?? GetFilterValue(filter, "FromDate") 
                   ?? GetFilterValue(filter, "FromDt");

        var to = GetFilterValue(filter, "To") 
                 ?? GetFilterValue(filter, "ToDate") 
                 ?? GetFilterValue(filter, "ToDt");

        if (from is not null && Compare(actual, from) < 0)
            return false;

        if (to is not null && Compare(actual, to) > 0)
            return false;

        return true;
    }

    private static object? GetFilterValue<TFilter>(TFilter filter, string propertyName)
    {
        return typeof(TFilter).GetProperty(propertyName)?.GetValue(filter);
    }

    private static int Compare(object left, object right)
    {
        if (left is Instant li && right is Instant ri)
            return li.CompareTo(ri);

        if (left is LocalDate ld && right is LocalDate rd)
            return ld.CompareTo(rd);

        if (left is IComparable comparable)
            return comparable.CompareTo(right);

        throw new InvalidOperationException(
            $"Values of type {left.GetType().Name} are not comparable.");
    }
}