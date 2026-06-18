using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Tests.Mocks;

public class MockQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
{
    public TResult Result { get; set; }
    
    public TResult Handle(TQuery query) => Result;
}