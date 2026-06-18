using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
	{
		public void Configure(EntityTypeBuilder<Currency> builder)
		{
			builder.ToTable("currencies", "static_data");
			builder.HasKey(c => c.CurrencyId);
			builder.Property(c => c.CurrencyId).HasColumnName("currency_id").IsRequired();
			builder.Property(c => c.Decimals).HasColumnName("decimals").IsRequired();
			builder.HasOne(a => a.Asset)
				.WithOne()
				.HasForeignKey<Currency>(a => a.CurrencyId)
				.OnDelete(DeleteBehavior.Restrict);
		}
		
		public static void CreateRelations(ModelBuilder modelBuilder)
		{
            modelBuilder.Entity<Currency>().HasData(new Currency
            {
	            CurrencyId = 840,
	            Decimals = 2
            });
		}
	}
}

