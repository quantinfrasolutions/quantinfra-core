using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class TradingSessionIntervalConfiguration : IEntityTypeConfiguration<TradingSessionInterval>
{		
    public void Configure(EntityTypeBuilder<TradingSessionInterval> builder)
    {
        builder.ToTable("trading_session_intervals", "static_data");
        builder.HasKey(a => a.IntervalId);
        builder.Property(a => a.IntervalId).HasColumnName("interval_id")
            .HasDefaultValueSql("nextval('static_data.ts_intervals_seq')")
            .IsRequired();
        builder.Property(a => a.TradingSessionId).HasColumnName("trading_session_id").IsRequired();
        builder.HasOne<TradingSession>()
            .WithMany(ts => ts.Days)
            .HasForeignKey(a => a.TradingSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.StartDay).HasColumnName("start_day").IsRequired();
        builder.Property(a => a.Start).HasColumnName("start").IsRequired();
        builder.Property(a => a.EndDay).HasColumnName("end_day").IsRequired();
        builder.Property(a => a.End).HasColumnName("end").IsRequired();
    }

    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("ts_intervals_seq", "static_data")
            .StartsAt(10000);
    }
}