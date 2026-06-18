using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Databases.Main.Models.Infrastructure;

namespace QuantInfra.Databases.Main.Models.MarketData;

public class BinanceUsdmSubscriptionConfiguration : IEntityTypeConfiguration<BinanceUsdmMarketDataSubscription>
{
	public void Configure(EntityTypeBuilder<BinanceUsdmMarketDataSubscription> builder)
	{
		builder.ToTable("binance_usdm_subscriptions", "market_data");
		builder.HasKey(a => a.SubscriptionId);
		builder.Property(a => a.SubscriptionId).HasColumnName("subscription_id").IsRequired();
		builder.Property(a => a.StreamId).HasColumnName("stream_id");
		builder.Property(a => a.SubscriptionType).HasColumnName("subscription_type").HasColumnType("text").IsRequired();
		builder.Property(a => a.Symbol).HasColumnName("symbol").IsRequired();
		builder.Property(a => a.ClientName).HasColumnName("client_name").IsRequired();
		builder.Ignore(a => a.LastBar);
		
		builder.HasOne<QuantInfra.Sdk.StaticData.Stream>()
			.WithMany()
			.HasForeignKey(a => a.StreamId)
			.OnDelete(DeleteBehavior.Restrict);
		
		builder.HasOne<MarketDataClientInstanceModel>()
			.WithMany()
			.HasForeignKey(a => a.ClientName)
			.OnDelete(DeleteBehavior.Restrict);
	}
}