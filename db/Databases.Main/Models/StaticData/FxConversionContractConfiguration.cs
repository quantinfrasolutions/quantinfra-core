using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class FxConversionContractConfiguration : IEntityTypeConfiguration<FxConversionContract>
{
    public void Configure(EntityTypeBuilder<FxConversionContract> builder)
    {
        builder.ToTable("fx_conversion_contracts", "static_data");
        builder.HasKey(a => a.ContractId);
        builder.Property(a => a.ContractId).HasColumnName("contract_id").IsRequired();
        builder.HasOne(a => a.Contract)
            .WithMany()
            .HasForeignKey(c => c.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}