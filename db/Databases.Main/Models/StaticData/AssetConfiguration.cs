using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
	public void Configure(EntityTypeBuilder<Asset> builder)
	{
		builder.ToTable("assets", "static_data");
		builder.HasKey(a => a.AssetId);
		builder.Property(a => a.AssetId).HasColumnName("asset_id")
			.HasDefaultValueSql("nextval('static_data.assets_seq')")
			.IsRequired();
		builder.Property(a => a.Name).HasColumnName("name").IsRequired();
		builder.Property(a => a.Description).HasColumnName("description");
		builder.Property(a => a.AssetType).HasColumnName("type").HasColumnType("text").IsRequired();
	}
		
	public static void CreateRelations(ModelBuilder modelBuilder)
	{
		modelBuilder.HasSequence<int>("assets_seq", "static_data")
			.StartsAt(100000);
			
		modelBuilder.Entity<Asset>().HasIndex(a => a.Name).IsUnique();

		modelBuilder.Entity<Asset>().HasData(new Asset
		{
			AssetType = AssetType.Currency,
			AssetId = 840,
			Name = "USD",
			Description = "US Dollar"
		});
	}
}