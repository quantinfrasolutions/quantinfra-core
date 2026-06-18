using Common.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;

namespace QuantInfra.Databases.Main.Models.Infrastructure;

public class MarketDataClientInstanceModel : MarketDataClientInstance
{
    public LocationModel LocationModel { get; set; } = null!;
}

public class MarketDataClientInstanceConfiguration : IEntityTypeConfiguration<MarketDataClientInstanceModel>
{
    public void Configure(EntityTypeBuilder<MarketDataClientInstanceModel> builder)
    {
        builder.ToTable("market_data_clients", "infrastructure");
        builder.HasKey(a => a.Name);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.LocationName).HasColumnName("location_name").IsRequired();
        builder
            .HasOne(a => a.LocationModel)
            .WithMany()
            .HasForeignKey(x => x.LocationName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}