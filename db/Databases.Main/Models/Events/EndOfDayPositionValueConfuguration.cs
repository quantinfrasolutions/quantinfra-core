using Common.Trading.Positions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;

namespace QuantInfra.Databases.Main.Models.Events;

public class EndOfDayPositionValueConfuguration : IEntityTypeConfiguration<PositionValue>
{
    public void Configure(EntityTypeBuilder<PositionValue> builder)
    {
        builder.ToTable("end_of_day_positions", "events");
        
        builder.HasKey(a => new { a.AccountId, a.PositionId, a.Dt });
        
        builder.Property<long>("event_id").HasColumnName("event_id").IsRequired();
        builder.Property<string>("account_service_name").HasColumnName("account_service_name").IsRequired();
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.PositionId).HasColumnName("position_id").IsRequired();
        builder.Property(a => a.Dt).HasColumnName("dt").IsRequired();
        builder.Property(a => a.Price).HasColumnName("price").IsRequired();
        builder.Property(a => a.SignedValue).HasColumnName("signed_value").IsRequired();
        builder.Property(a => a.FxRate).HasColumnName("fx_rate").IsRequired();
        builder.Property(a => a.SignedValueInAccountCcy).HasColumnName("signed_value_in_account_ccy").IsRequired();
        builder.Property(a => a.EquityValueInAccountCcy).HasColumnName("equity_value_in_account_ccy").IsRequired();
        
        builder.HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<Event>()
            .WithMany(e => e.EndOfDayPositions)
            .HasForeignKey("account_service_name", "event_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}