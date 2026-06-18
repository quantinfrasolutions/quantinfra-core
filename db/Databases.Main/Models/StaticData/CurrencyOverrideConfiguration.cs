using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public class CurrencyOverrideConfiguration : IEntityTypeConfiguration<CurrencyOverride>
	{
		public void Configure(EntityTypeBuilder<CurrencyOverride> builder)
		{
			builder.ToTable("currency_overrides", "static_data");
			builder.HasKey(c => new { c.CurrencyId, c.BrokerId });
			builder.Property(c => c.CurrencyId).HasColumnName("currency_id").IsRequired();
			builder.Property(c => c.BrokerId).HasColumnName("broker_id").IsRequired();
			builder.Property(c => c.Decimals).HasColumnName("decimals").IsRequired();
			
			builder.HasOne<Currency>()
				.WithMany(c => c.BrokerOverrides)
				.HasForeignKey(c => c.CurrencyId)
				.OnDelete(DeleteBehavior.Restrict);
			
			builder.HasOne<Broker>()
				.WithMany()
				.HasForeignKey(a => a.BrokerId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}

