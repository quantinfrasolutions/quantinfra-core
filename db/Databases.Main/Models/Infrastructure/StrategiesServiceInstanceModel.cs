using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Databases.Main.Models.Entities;

namespace QuantInfra.Databases.Main.Models.Infrastructure;

public class StrategiesServiceInstanceModel : StrategiesServiceInstance
{
    public LocationModel LocationModel { get; set; } = null!;
    public ICollection<StrategyModel> Strategies { get; set; } = null!;
}

public class StrategiesServiceInstanceConfiguration : IEntityTypeConfiguration<StrategiesServiceInstanceModel>
{
    public void Configure(EntityTypeBuilder<StrategiesServiceInstanceModel> builder)
    {
        builder.ToTable("ss_instances", "infrastructure");
        builder.HasKey(a => a.Name);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.LocationName).HasColumnName("location_name").IsRequired();
        builder
            .HasOne(a => a.LocationModel)
            .WithMany(l => l.StrategyServiceInstances)
            .HasForeignKey(x => x.LocationName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}