using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class TradingSessionConfiguration : IEntityTypeConfiguration<TradingSession>
{
	public void Configure(EntityTypeBuilder<TradingSession> builder)
	{
		builder.ToTable("trading_sessions", "static_data");
		builder.HasKey(a => a.TradingSessionId);
		builder.Property(a => a.TradingSessionId).HasColumnName("trading_session_id")
			.HasDefaultValueSql("nextval('static_data.trading_sessions_seq')")
			.IsRequired();
		builder.Property(a => a.Name).HasColumnName("name").IsRequired();
		builder.Property(ts => ts.ExchangeId).HasColumnName("exchange_id").IsRequired();
		builder
			.HasOne(a => a.Exchange)
			.WithMany(e => e.TradingSessions)
			.HasForeignKey(a => a.ExchangeId)
			.OnDelete(DeleteBehavior.Restrict);
		builder.Property(a => a.Is24X7).HasColumnName("is_24x7").IsRequired();
		builder.Property(a => a.IsRth).HasColumnName("is_rth").IsRequired();
	}

	public static void CreateRelations(ModelBuilder modelBuilder)
	{
		modelBuilder.HasSequence<int>("trading_sessions_seq", "static_data")
			.StartsAt(1000);
	}
}