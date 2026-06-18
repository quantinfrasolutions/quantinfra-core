using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Databases.Main.Models.Events;

public class ExternalTradeModel : ExternalTradeRecord
{
    public ExternalTradeModel() { }
    
    public ExternalTradeModel(ExternalTradeRecord trade, string accountServiceName, long eventId) : base(trade)
    {
        AccountServiceName = accountServiceName;
        EventId = eventId;
    }

    public string AccountServiceName { get; init; }
    public long EventId { get; init; }
    public AccountModel Account { get; init; }
    public Event Event { get; init; }
}

public class ExternalTradeConfiguration : IEntityTypeConfiguration<ExternalTradeModel>
{
    public void Configure(EntityTypeBuilder<ExternalTradeModel> builder)
    {
        builder.ToTable("external_trades", "events");
        
        builder.Property(a => a.AccountServiceName).HasColumnName("account_service_name").IsRequired();
        builder.Property(a => a.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.ExternalTradeId).HasColumnName("external_trade_id");
        builder.Property(a => a.ExternalContractId).HasColumnName("external_contract_id");
        builder.Property(a => a.Side).HasColumnName("side").HasColumnType("text").IsRequired();
        builder.Property(a => a.Volume).HasColumnName("volume").IsRequired();
        builder.Property(a => a.Price).HasColumnName("price").IsRequired();
        builder.Property(a => a.Commission).HasColumnName("commission").IsRequired();
        builder.Property(a => a.Dt).HasColumnName("dt").IsRequired();
        builder.Property(a => a.ExternalOrderId).HasColumnName("external_order_id");
        builder.Property(a => a.CommissionCurrency).HasColumnName("commission_currency");
        builder.Property(a => a.CalculatedCcyLastQty).HasColumnName("calculated_ccy_last_qty").IsRequired();
        builder.Ignore(a => a.SignedVolume);
        
        builder.HasKey(t => new { t.AccountId, t.ExternalTradeId });
        
        
        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.Event)
            .WithOne(e => e.ExternalTrade)
            .HasForeignKey<ExternalTradeModel>(t => new { t.AccountServiceName, t.EventId })
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<ExternalTradeRecord>();
    }
}