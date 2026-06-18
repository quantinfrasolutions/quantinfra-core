using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Databases.Main.Models.Projections;

public class SharePriceHistoryConfiguration : IEntityTypeConfiguration<SharePriceHistory>
{
    public void Configure(EntityTypeBuilder<SharePriceHistory> builder)
    {
        builder.ToTable("share_price_history", "projections");
        
        builder.Property<long>("event_id").HasColumnName("event_id").IsRequired();
        builder.Property<string>("account_service_name").HasColumnName("account_service_name").IsRequired();
        
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.Dt).HasColumnName("dt").IsRequired();
        builder.Property(a => a.ShareCount).HasColumnName("share_count").IsRequired();
        builder.Property(a => a.SharePrice).HasColumnName("share_price").IsRequired();
        builder.Property(a => a.DailyReturn).HasColumnName("daily_return").IsRequired();
        builder.Property(a => a.HWM).HasColumnName("hwm").IsRequired();
        builder.Property(a => a.Investment).HasColumnName("investment").IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasColumnType("text").IsRequired();
        
        builder.HasKey(a => new { a.AccountId, a.Dt });
        
        builder.HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey("account_service_name", "event_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}