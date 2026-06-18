using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Databases.Main.Models.Infrastructure;

namespace QuantInfra.Databases.Main.Models.MarketData;

public class BinanceUsdmOrderBookSubscriptionConfiguration : IEntityTypeConfiguration<BinanceUsdmOrderBookSubscription>
{
	public void Configure(EntityTypeBuilder<BinanceUsdmOrderBookSubscription> builder)
	{
		builder.ToTable("binance_usdm_ob_subscriptions", "market_data");
		builder.HasKey(a => a.SubscriptionId);
		builder.Property(a => a.SubscriptionId).HasColumnName("subscription_id").IsRequired();
		builder.Property(a => a.ContractId).HasColumnName("contract_id").IsRequired();
		builder.Property(a => a.Symbol).HasColumnName("symbol").IsRequired();
		builder.Property(a => a.Frequency).HasColumnName("frequency").IsRequired();
		builder.Property(a => a.Levels).HasColumnName("levels").IsRequired();
		builder.Property(a => a.ClientName).HasColumnName("client_name").IsRequired();
		builder.Ignore(a => a.LastUpdate);
		
		builder.HasOne<QuantInfra.Sdk.StaticData.Contract>()
			.WithMany()
			.HasForeignKey(a => a.ContractId)
			.OnDelete(DeleteBehavior.Restrict);
		
		builder.HasOne<MarketDataClientInstanceModel>()
			.WithMany()
			.HasForeignKey(a => a.ClientName)
			.OnDelete(DeleteBehavior.Restrict);
	}
}