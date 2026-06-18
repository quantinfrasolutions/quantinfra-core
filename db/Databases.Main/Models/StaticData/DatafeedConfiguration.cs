using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public class DatafeedConfiguration : IEntityTypeConfiguration<Datafeed>
	{
		public void Configure(EntityTypeBuilder<Datafeed> builder)
		{
			builder.ToTable("datafeeds", "static_data");
			builder.HasKey(d => d.DatafeedId);
			builder.Property(d => d.DatafeedId).HasColumnName("datafeed_id")
				.HasDefaultValueSql("nextval('static_data.datafeeds_seq')")
				.IsRequired();
			builder.Property(a => a.Name).HasColumnName("name").IsRequired();
		}

        public static void CreateRelations(ModelBuilder modelBuilder)
		{
			modelBuilder.HasSequence<int>("datafeeds_seq", "static_data")
				.StartsAt(100);
			
			modelBuilder.Entity<Datafeed>().HasIndex(d => d.Name).IsUnique();
        }
	}
}

