namespace QuantInfra.Common.Utils.Collections;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source) =>
        source.ToDictionary(x => x.Key, x => x.Value);

    public static IReadOnlyDictionary<TKey, TValue> Copy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
        CopyAsDictionary(source);
    
    public static Dictionary<TKey, TValue> CopyAsDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source) =>
        source.ToDictionary(x => x.Key, x => x.Value);
    
    public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this Dictionary<TKey, TValue> source) =>
        source.ToDictionary(x => x.Key, x => x.Value);

    public static Dictionary<TKey, TValue> CopyOrEmpty<TKey, TValue>(this IDictionary<TKey, TValue>? source) =>
        source?.Copy() ?? new Dictionary<TKey, TValue>();
    
    public static Dictionary<TKey, TValue> CopyOrEmpty<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? source) =>
        source?.CopyAsDictionary() ?? new Dictionary<TKey, TValue>();

    public static string SerializeDictionary<K, V>(this IDictionary<K, V>? source) => source == null
        ? "null"
        : string.Join(',', source.Select(kv => $"{kv.Key}: {kv.Value}"));

    public static void AddRange<TK, TV>(this IDictionary<TK, TV> dict, IDictionary<TK, TV> range)
    {
        foreach (var kv in range)
        {
            dict.Add(kv.Key, kv.Value);
        }
    }
    
    public static void AddRange<TK, TV>(this IDictionary<TK, TV> dict, IReadOnlyDictionary<TK, TV> range)
    {
        foreach (var kv in range)
        {
            dict.Add(kv.Key, kv.Value);
        }
    }
}