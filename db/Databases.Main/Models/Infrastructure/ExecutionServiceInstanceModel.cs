using System.ComponentModel.DataAnnotations.Schema;
using Common.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Databases.Main.Models.Infrastructure;

[Table("es_instances", Schema = "infrastructure")]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ExecutionServiceInstanceModel : ExecutionServiceInstance
{
    public LocationModel LocationModel { get; set; } = null!;
    public ICollection<TradingClientConfig> TradingClients { get; set; } = null!;
}

public class ExecutionServiceInstanceConfiguration : IEntityTypeConfiguration<ExecutionServiceInstanceModel>
{
    public void Configure(EntityTypeBuilder<ExecutionServiceInstanceModel> builder)
    {
        builder.ToTable("es_instances", "infrastructure");
        builder.HasKey(a => a.Name);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.LocationName).HasColumnName("location_name").IsRequired();
        builder
            .HasOne(a => a.LocationModel)
            .WithMany(l => l.ExecutionServiceInstances)
            .HasForeignKey(x => x.LocationName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}