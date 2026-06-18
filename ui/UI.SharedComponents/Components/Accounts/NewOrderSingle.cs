using Common.Trading;
using NodaTime;
using QuantInfra.Sdk.Trading.Orders;

namespace UI.SharedComponents.Components.Accounts;

public class NewOrderSingle
{
    public string? ClOrdId { get; set; }

    public int AccountId { get; set; }
    public int ContractId { get; set; }
    public string? StrategyPositionId { get; set; }
    public PositionEffect? PositionEffect { get; set; }
    public long? SignalGroupId { get; set; }
    public long? ExecutionRequestId { get; set; }
    public long? ParentPositionId { get; set; }
    
    public OrdType OrdType { get; set; }
    public Side Side { get; set; }
    public decimal OrderQty { get; set; }

    public decimal? Price { get; set; }
    public decimal? StopPx { get; set; }
        
    public TimeInForce TimeInForce { get; set; }
    public bool IsSuspended { get; set; }
    public Instant? CreatedAt { get; set; }
    public Instant? ActivationDt { get; set; }
    public Instant? ExpireDt { get; set; }

    public Dictionary<string, LinkType> LinkedOrders { get; set; } = new();
    public List<int>? TradingSessionsIds { get; set; } = new();

    public PegInstructions? PegInstructions { get; set; }
    public bool IsSltp { get; set; }
    public List<ExecInst> ExecInst { get; set; } = new();
    
    public List<Allocation>? Allocations { get; set; }

    public QuantInfra.Sdk.Trading.Orders.NewOrderSingle ToNewOrderSingle() => new(AccountId, ClOrdId,
        ContractId, StrategyPositionId, PositionEffect, SignalGroupId, ExecutionRequestId, ParentPositionId,
        OrdType, Side, OrderQty, Price, StopPx, TimeInForce, IsSuspended, CreatedAt, ActivationDt, ExpireDt,
        TradingSessionsIds, PegInstructions, IsSltp, Allocations, new Dictionary<string, LinkType>());
}