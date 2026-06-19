using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Databases.Main.Models.History;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Databases.Main.Models.Projections;

public class PositionHistoryModel : Position
{
    public PositionHistoryModel() { }
    
    public PositionHistoryModel(Position position, string asName, long eventId, PositionChangeType type)
        : base(position)
    {
        AccountServiceName = asName;
        EventId = eventId;
        Type = type;
    }

    public string AccountServiceName { get; init; }
    public long EventId { get; init; }
    public PositionChangeType Type { get; init; }
    public AccountModel Account { get; init; }
    public TradeModel OpenTrade { get; init; }
    public TradeModel CloseTrade { get; init; }
    public Event Event { get; init; }
    public Contract Contract { get; init; }
}

public class PositionHistoryConfiguration : IEntityTypeConfiguration<PositionHistoryModel>
{
    public void Configure(EntityTypeBuilder<PositionHistoryModel> builder)
    {
        builder.ToTable("positions_history", "projections");
        
        
        builder.Property(p => p.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(p => p.AccountServiceName).HasColumnName("account_service_name").IsRequired();
        builder.Property(p => p.Type).HasColumnName("change_type").HasColumnType("text").IsRequired();
        
        builder.Property<long>("position_history_id").HasColumnName("position_history_id").IsRequired()
            .HasDefaultValueSql("nextval('history.position_history_id_seq')").IsRequired();
        builder.Property(a => a.OpenTradeId).HasColumnName("open_trade_id").IsRequired();
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.StrategyPositionId).HasColumnName("strategy_position_id");
        builder.Property(a => a.ContractId).HasColumnName("contract_id").IsRequired();
        builder.Property(a => a.Volume).HasColumnName("volume").IsRequired();
        builder.Property(a => a.Side).HasColumnName("side").HasColumnType("text").IsRequired();
        builder.Ignore(a => a.OpenPrice);
        builder.Property(a => a.TotalOpenPayments).HasColumnName("total_open_payments").IsRequired();
        builder.Property(a => a.OpenDt).HasColumnName("open_dt").IsRequired();
        builder.Property(a => a.HistoryOpenDt).HasColumnName("history_open_dt").IsRequired();
        builder.Property(a => a.TotalSettlPayments).HasColumnName("total_settl_payments").IsRequired();
        builder.Property(a => a.TotalSettlPaymentsInAccountCcy).HasColumnName("total_settl_payments_in_account_ccy").IsRequired();
        builder.Ignore(a => a.SettlPrice);
        builder.Property(a => a.CloseTradeId).HasColumnName("close_trade_id");
        builder.Property(a => a.ClosePrice).HasColumnName("close_price");
        builder.Property(a => a.CloseDt).HasColumnName("close_dt");
        builder.Ignore(a => a.IsClosed);
        builder.Property(a => a.RealizedPnL).HasColumnName("realized_pnl").IsRequired();
        builder.Property(a => a.RealizedPnLInAccountCcy).HasColumnName("realized_pnl_in_account_ccy").IsRequired();
        builder.Property(a => a.FloatingPnL).HasColumnName("floating_pnl").IsRequired();
        builder.Property(a => a.TotalFloatingPnL).HasColumnName("total_floating_pnl").IsRequired();
        builder.Property(a => a.Commission).HasColumnName("commission").IsRequired();
        builder.Property(a => a.SignalGroupId).HasColumnName("signal_group_id");
        builder.Property(a => a.IsSynthetic).HasColumnName("is_synthetic").IsRequired();
        builder.Property(a => a.ParentPositionId).HasColumnName("parent_position_id");
        builder.Ignore(a => a.SignedVolume);

        builder.HasKey("position_history_id");
        
        builder.HasOne(p => p.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.OpenTrade)
            .WithMany()
            .HasForeignKey(p => new { p.AccountServiceName, p.OpenTradeId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.CloseTrade)
            .WithMany()
            .HasForeignKey(p => new { p.AccountServiceName, p.CloseTradeId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.Event)
            .WithMany()
            .HasForeignKey(p => new { p.AccountServiceName, p.EventId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.Contract)
            .WithMany()
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Position>();
        
        modelBuilder.HasSequence<long>("position_history_id_seq", "history")
            .StartsAt(100000000);
    }
}