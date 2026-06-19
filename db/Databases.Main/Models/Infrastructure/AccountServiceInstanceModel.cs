using Common.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Databases.Main.Models.Entities;

namespace QuantInfra.Databases.Main.Models.Infrastructure;

public class AccountServiceInstanceModel : AccountServiceInstance
{
    public LocationModel LocationModel { get; set; } = null!;
    public ICollection<AccountModel> Accounts { get; set; } = null!;
}

public class AccountServiceInstanceConfiguration : IEntityTypeConfiguration<AccountServiceInstanceModel>
{
    public void Configure(EntityTypeBuilder<AccountServiceInstanceModel> builder)
    {
        builder.ToTable("as_instances", "infrastructure");
        builder.HasKey(a => a.Name);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.LocationName).HasColumnName("location_name").IsRequired();
        builder
            .HasOne(a => a.LocationModel)
            .WithMany(l => l.AccountServiceInstances)
            .HasForeignKey(x => x.LocationName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}