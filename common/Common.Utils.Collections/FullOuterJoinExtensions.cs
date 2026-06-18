namespace Common.Utils.Collections;

public static class FullOuterJoinExtension
{
    public static IList<TR> FullOuterGroupJoin<TA, TB, TK, TR>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TK> selectKeyA,
            Func<TB, TK> selectKeyB,
            Func<IEnumerable<TA>, IEnumerable<TB>, TK, TR> projection,
            IEqualityComparer<TK> cmp = null)
    {
        cmp = cmp ?? EqualityComparer<TK>.Default;
        var alookup = a.ToLookup(selectKeyA, cmp);
        var blookup = b.ToLookup(selectKeyB, cmp);

        var keys = new HashSet<TK>(alookup.Select(p => p.Key), cmp);
        keys.UnionWith(blookup.Select(p => p.Key));

        var join = from key in keys
                   let xa = alookup[key]
                   let xb = blookup[key]
                   select projection(xa, xb, key);

        return join.ToList();
    }

    public static IList<TR> FullOuterJoin<TA, TB, TK, TR>(
        this IEnumerable<TA> a,
        IEnumerable<TB> b,
        Func<TA, TK> selectKeyA,
        Func<TB, TK> selectKeyB,
        Func<TA, TB, TK, TR> projection,
        TA defaultA = default(TA),
        TB defaultB = default(TB),
        IEqualityComparer<TK> cmp = null)
    {
        cmp = cmp ?? EqualityComparer<TK>.Default;
        var alookup = a.ToLookup(selectKeyA, cmp);
        var blookup = b.ToLookup(selectKeyB, cmp);

        var keys = new HashSet<TK>(alookup.Select(p => p.Key), cmp);
        keys.UnionWith(blookup.Select(p => p.Key));

        var join = from key in keys
                   from xa in alookup[key].DefaultIfEmpty(defaultA)
                   from xb in blookup[key].DefaultIfEmpty(defaultB)
                   select projection(xa, xb, key);

        return join.ToList();
    }

    public static IList<TR> FullOuterJoinWithNulls<TA, TB, TK, TR>(
        this IEnumerable<TA> a,
        IEnumerable<TB> b,
        Func<TA, TK> selectKeyA,
        Func<TB, TK> selectKeyB,
        Func<TA?, TB?, TK, TR> projection,            
        IEqualityComparer<TK> cmp = null
    )
        where TA : struct
        where TB : struct
    {
        cmp = cmp ?? EqualityComparer<TK>.Default;
        var aDict = a.ToDictionary(selectKeyA, cmp);
        var bDict = b.ToDictionary(selectKeyB, cmp);

        var keys = new HashSet<TK>(aDict.Select(p => p.Key), cmp);
        keys.UnionWith(bDict.Select(p => p.Key));

        var join = keys
            .Select(k => projection(
                aDict.ContainsKey(k) ? aDict[k] : null,
                bDict.ContainsKey(k) ? bDict[k] : null,
                k
            ));

        return join.ToList();
    }
}