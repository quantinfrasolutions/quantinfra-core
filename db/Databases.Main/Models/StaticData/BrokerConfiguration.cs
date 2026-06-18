using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
	{
		public void Configure(EntityTypeBuilder<Broker> builder)
		{
			builder.ToTable("brokers", "static_data");
			builder.HasKey(b => b.BrokerId);
			builder.Property(b => b.BrokerId).HasColumnName("broker_id")
				.HasDefaultValueSql("nextval('static_data.brokers_seq')")
				.IsRequired();
			builder.Property(b => b.Name).HasColumnName("name").IsRequired();
			builder.Property(b => b.BrokerType).HasColumnName("broker_type")
				.HasColumnType("text").IsRequired();
		}
		
		
		public static void CreateRelations(ModelBuilder modelBuilder)
		{
			modelBuilder.HasSequence<int>("brokers_seq", "static_data")
				.StartsAt(100);
			
			modelBuilder.Entity<Broker>().HasIndex(b => b.Name).IsUnique(true);
		}
	}
}

