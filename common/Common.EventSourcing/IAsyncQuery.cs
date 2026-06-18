using System;

namespace QuantInfra.Common.EventSourcing;

public interface IAsyncQuery
{
    Guid RequestId { get; }   
}

public interface IAsyncQuery<TResult> : IQuery<TResult>, IAsyncQuery
{ }

public interface IAsyncQueryWithMulticast<TResult> : IAsyncQuery<TResult>
{
    bool UseMulticast { get; }
}