using Common.Trading;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base;

namespace Domain.Accounts.Base.Tests;

class MockIdsProvider : IEventIdProvider
{
    public long Id { get; set; }
    public long GetNextEventId() => ++Id;
}

class MockBOIdProvider : IBalanceOperationIdProvider
{
    public int Id { get; set; }
    public int GetNextBalanceOperationId() => ++Id;
}

class MockOrderIdProvider : IOrderIdProvider
{
    public long Id { get; set; }
    public long GetNextOrderId() => ++Id;
}

class MockExecIdProvider : IExecIdProvider
{
    public long Id { get; set; }
    public long GetNextExecId() => ++Id;
}

class MockTradeIdProvider : ITradeIdProvider
{
    public long Id { get; set; }
    public long GetNextTradeId() => ++Id;
}