using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;

namespace QuantInfra.Databases.Main.Models.History;

public class TradeModel : Trade
{
    public TradeModel() { }
    
    public TradeModel(Trade trade, string accountServiceName) : base(trade)
    {
        AccountServiceName = accountServiceName;
    }
    
    public Contract Contract { get; init; }
    public AccountModel Account { get; init; }
    public Event Event { get; init; }
    public ExecutionReportModel? ExecutionReport { get; init; }
    public Currency PaymentCurrency { get; init; }
}

public class TradeConfiguration : IEntityTypeConfiguration<TradeModel>
{
    public void Configure(EntityTypeBuilder<TradeModel> builder)
    {
        builder.ToTable("trades", "history");
        
        builder.Property(a => a.AccountServiceName).HasColumnName("account_service_name").IsRequired();
        builder.Property(a => a.TradeId).HasColumnName("trade_id").IsRequired();
        builder.Property(a => a.ExternalTradeId).HasColumnName("external_trade_id");
        builder.Property(a => a.OrigTradeId).HasColumnName("orig_trade_id");
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.ClOrdId).HasColumnName("cl_ord_id");
        builder.Property(a => a.ContractId).HasColumnName("contract_id").IsRequired();
        builder.Property(a => a.Dt).HasColumnName("dt").IsRequired();
        builder.Property(a => a.OrderId).HasColumnName("order_id");
        builder.Property(a => a.ExecId).HasColumnName("exec_id");
        builder.Property(a => a.StrategyPositionId).HasColumnName("strategy_position_id");
        builder.Property(a => a.SignalGroupId).HasColumnName("signal_group_id");
        builder.Property(a => a.PositionEffect).HasColumnName("position_effect").HasColumnType("text");
        builder.Property(a => a.Side).HasColumnName("side").HasColumnType("text").IsRequired();
        builder.Property(a => a.Volume).HasColumnName("volume").IsRequired();
        builder.Property(a => a.Price).HasColumnName("price").IsRequired();
        builder.Property(a => a.Commission).HasColumnName("commission").IsRequired();
        builder.Property(a => a.ExecutionRequestId).HasColumnName("execution_request_id");
        builder.Property(a => a.Commissions).HasColumnName("commissions").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.PaymentCurrencyId).HasColumnName("payment_currency_id").IsRequired();
        builder.Property(a => a.FxRate).HasColumnName("fx_rate").IsRequired();
        builder.Property(a => a.CalculatedCcyLastQty).HasColumnName("calculated_ccy_last_qty").IsRequired();
        builder.Property(a => a.ParentPositionId).HasColumnName("parent_position_id");
        builder.Property(a => a.IsSynthetic).HasColumnName("is_synthetic").IsRequired();
        builder.Ignore(a => a.SignedVolume);
        
        // TradeId is unique only within one AS instance
        builder.HasKey(t => new { t.AccountServiceName, t.TradeId });
        
        
        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.Event)
            .WithOne(e => e.Trade)
            .HasForeignKey<Event>(t => new { t.AccountServiceName, t.TradeId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.Contract)
            .WithMany()
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.ExecutionReport)
            .WithMany()
            .HasForeignKey(t => new { t.AccountServiceName, t.ExecId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.PaymentCurrency)
            .WithMany()
            .HasForeignKey(a => a.PaymentCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Trade>();
    }
}