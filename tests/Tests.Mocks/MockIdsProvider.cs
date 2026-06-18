using Common.Trading;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base;

namespace QuantInfra.Tests.Mocks;

public class MockIdsProvider : 
    IEventIdProvider,
    IBalanceOperationIdProvider,
    IOrderIdProvider,
    IExecIdProvider,
    ITradeIdProvider
    // ,
    // IExecutionRequestIdProvider,
    // ITargetPositionHistoryIdProvider
{
    public long EventId { get; set; } = 100000000;
    public long GetNextEventId() => EventId++;

    public int BalanceOperationId { get; set; } = 10000;
    public int GetNextBalanceOperationId() => BalanceOperationId++;

    public long OrderId { get; set; } = 1000000;
    public long GetNextOrderId() => OrderId++;

    public long ExecId { get; set; } = 10000000;
    public long GetNextExecId() => ExecId++;

    public long TradeId { get; set; } = 1000000;
    public long GetNextTradeId() => TradeId++;
    
    public long ExecutionRequestId { get; set; } = 100000;
    public long GetNextExecutionRequestId() => ExecutionRequestId++;

    public long ExecutionRequestStatusId { get; set; } = 100000;
    public long GetNextExecutionRequestStatusId() => ExecutionRequestStatusId++;
    
    public long TargetPositionHistoryId { get; set; } = 1000000;
    public long GetNextTargetPositionHistoryId() => TargetPositionHistoryId++;
}