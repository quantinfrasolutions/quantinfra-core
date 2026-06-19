using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.History;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Databases.Main.Models.Events;

[Table("events", Schema = "events")]
public class Event : IEvent
{
    public Event() { }
    
    public Event(string accountServiceName, long eventId, string eventType, Instant timestamp, long version)
    {
        AccountServiceName = accountServiceName;
        EventId = eventId;
        EventType = eventType;
        Timestamp = timestamp;
        Version = version;
    }

    [Column("account_service_name"), Required] public string AccountServiceName { get; init; }
    public AccountServiceInstanceModel AccountServiceInstanceModel { get; init; }
    
    [Column("event_id"), Required] public long EventId { get; init; }
    [Column("event_type"), Required] public string EventType { get; init; }
    [Column("ts")] public Instant Timestamp { get; init; }
    
    [Column("account_id")] public int? AccountId { get; init; }
    public AccountModel? Account { get; init; }
    [Column("strategy_id")] public int? StrategyId { get; init; }
    public StrategyModel? Strategy { get; init; }
    [Column("version"), Required] public long Version { get; init; }
    
    [Column("balance_operation_id")] public int? BalanceOperationId { get; init; }
    public BalanceOperation? BalanceOperation { get; init; }
    public ICollection<BalanceValueModel> EndOfDayBalances { get; init; }
    public ICollection<PositionValue> EndOfDayPositions { get; init; }
    
    [Column("exec_id")] public long? ExecId { get; init; }
    public ExecutionReportModel? ExecutionReport { get; init; }
    
    public ShareCountUpdate? ShareCountUpdate { get; init; }
    public SharePriceUpdate? SharePriceUpdate { get; init; }
    
    [Column("trade_id")] public long? TradeId { get; init; }
    public TradeModel? Trade { get; init; }
    
    [Column("subaccount_id")] public int? SubaccountId { get; init; }
    public SubaccountModel? Subaccount { get; init; }
    
    [Column("data", TypeName = "jsonb")] public JsonDocument? Data { get; init; }
    
    public ExternalTradeModel? ExternalTrade { get; init; }

    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasKey(x => new { AccountServiceInstanceName = x.AccountServiceName, x.EventId });
        
        modelBuilder.Entity<Event>()
            .HasOne<AccountServiceInstanceModel>(x => x.AccountServiceInstanceModel)
            .WithMany()
            .HasForeignKey(x => x.AccountServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Event>()
            .HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Event>()
            .HasOne(x => x.Strategy)
            .WithMany()
            .HasForeignKey(x => x.StrategyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Subaccount)
            .WithMany()
            .HasForeignKey(x => x.SubaccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}