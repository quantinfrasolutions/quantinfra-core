using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Connectors.Ibkr.Interfaces;
using QuantInfra.Databases.Main.Models.Infrastructure;

namespace QuantInfra.Databases.Main.Models.MarketData;

public class IbkrSubscriptionConfiguration : IEntityTypeConfiguration<IbkrMarketDataSubscription>
{
	public void Configure(EntityTypeBuilder<IbkrMarketDataSubscription> builder)
	{
		builder.ToTable("ibkr_subscriptions", "market_data");
		builder.HasKey(a => a.SubscriptionId);
		builder.Property(a => a.SubscriptionId).HasColumnName("subscription_id").IsRequired();
		builder.Property(a => a.ConId).HasColumnName("con_id").IsRequired();
		builder.Property(a => a.Ticker).HasColumnName("ticker").IsRequired();
		builder.Property(a => a.SecurityType).HasColumnName("security_type").HasColumnType("text").IsRequired();
		builder.Property(a => a.Currency).HasColumnName("currency").IsRequired();
		builder.Property(a => a.Exchange).HasColumnName("exchange").IsRequired();
		builder.Property(a => a.FuturesLastDateOrContractMonth).HasColumnName("futures_last_date").IsRequired();
		builder.Property(a => a.LocalSymbol).HasColumnName("local_symbol").IsRequired();
		builder.Property(a => a.SubscriptionType).HasColumnName("subscription_type").HasColumnType("text").IsRequired();
		builder.Property(a => a.UseRTH).HasColumnName("use_rth").IsRequired();
		builder.Property(a => a.StreamId).HasColumnName("stream_id");
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