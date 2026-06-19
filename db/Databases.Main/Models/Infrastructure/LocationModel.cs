using Common.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Common.Infrastructure.Abstractions;

namespace QuantInfra.Databases.Main.Models.Infrastructure;

public class LocationModel : Location
{
    public ICollection<AccountServiceInstanceModel> AccountServiceInstances { get; set; } = null!;
    public ICollection<StrategiesServiceInstanceModel> StrategyServiceInstances { get; set; } = null!;
    public ICollection<ExecutionServiceInstanceModel>? ExecutionServiceInstances { get; set; } = null!;
}

public class LocationConfiguration : IEntityTypeConfiguration<LocationModel>
{
    public void Configure(EntityTypeBuilder<LocationModel> builder)
    {
        builder.ToTable("locations", "infrastructure");
        builder.HasKey(a => a.Name);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
    }
}