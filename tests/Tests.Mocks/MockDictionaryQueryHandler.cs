using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Tests.Mocks;

public class MockDictionaryQueryHandler<TQuery, TKey, TResult> : IQueryHandler<TQuery, TResult>
{
    public Dictionary<TKey, TResult> Result { get; set; } = new();
    public Func<TQuery, TKey> KeySelector { get; set; }
    
    public TResult Handle(TQuery query) => Result.GetValueOrDefault(KeySelector(query));
}