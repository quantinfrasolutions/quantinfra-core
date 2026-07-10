using System.ComponentModel.DataAnnotations;
using NodaTime;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace UI.SharedComponents.Accounts.Trading;

public class NewOrderSingleModel
{
    public string? ClOrdId { get; set; }

    public int AccountId { get; set; }
    public int ContractId { get; set; }
    public string? StrategyPositionId { get; set; }
    public PositionEffect? PositionEffect { get; set; }
    public long? SignalGroupId { get; set; }
    public long? ExecutionRequestId { get; set; }
    public long? ParentPositionId { get; set; }
    
    [Required(ErrorMessage = "OrdType is required")] 
    public OrdType OrdType { get; set; }
    
    [Required(ErrorMessage = "Side is required")] 
    [AllowedValues(Side.Buy, Side.Sell, ErrorMessage = "Side is required")] 
    public Side Side { get; set; }
    
    [Required(ErrorMessage = "OrderQty is required")]
    [Range(0, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "OrderQty must be greater than or equal to zero")]
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

    public NewOrderSingle ToNewOrderSingle() => new(AccountId, ClOrdId,
        ContractId, StrategyPositionId, PositionEffect, SignalGroupId, ExecutionRequestId, ParentPositionId,
        OrdType, Side, OrderQty, Price, StopPx, TimeInForce, IsSuspended, CreatedAt, ActivationDt, ExpireDt,
        TradingSessionsIds, PegInstructions, IsSltp, Allocations, new Dictionary<string, LinkType>());
}