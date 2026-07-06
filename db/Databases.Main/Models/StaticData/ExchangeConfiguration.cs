using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
	{
		public void Configure(EntityTypeBuilder<Exchange> builder)
		{
			builder.ToTable("exchanges", "static_data");
			builder.HasKey(e => e.ExchangeId);
			builder.Property(e => e.ExchangeId).HasColumnName("exchange_id")
				.HasDefaultValueSql("nextval('static_data.exchanges_seq')")
				.IsRequired();
			builder.Property(a => a.Name).HasColumnName("name").IsRequired();
			builder.Property(a => a.TimezoneName).HasColumnName("timezone").IsRequired();
		}

        public static void CreateRelations(ModelBuilder modelBuilder)
		{
			modelBuilder.HasSequence<int>("exchanges_seq", "static_data")
				.StartsAt(120);

			modelBuilder.Entity<Exchange>().HasData(
				new Exchange
				{
					ExchangeId = 119,
					Name = "Binance USD-m Futures",
				});
		}
	}
}

