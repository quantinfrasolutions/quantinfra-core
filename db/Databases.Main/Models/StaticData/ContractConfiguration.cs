using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts", "static_data");
        builder.HasKey(a => a.ContractId);
        builder.Property(a => a.ContractId).HasColumnName("contract_id")
            .HasDefaultValueSql("nextval('static_data.contracts_seq')")
            .IsRequired();
        builder.Property(a => a.Ticker).HasColumnName("ticker").IsRequired();
        builder.HasOne(c => c.Template)
            .WithMany()
            .HasForeignKey("template_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.FirstTradingDate).HasColumnName("first_trading_date");
        builder.Property(c => c.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(c => c.SyntheticContractType).HasColumnType("text").HasColumnName("synthetic_contract_type");
        builder.Property(c => c.SynthRequiresBarRecalculationAtRollover).HasColumnName("synthetic_requires_bar_recalculation_at_rollover");
        builder.Property(c => c.ExternalContractId).HasColumnName("external_contract_id");
        builder.HasOne(c => c.Asset)
            .WithMany()
            .HasForeignKey("asset_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.DefaultDatafeedId).HasColumnName("default_datafeed_id");
        // builder.HasOne(c => c.Stream)
        //     .WithMany()
        //     .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.SyntheticContractCompositionHistory); // TODO
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("contracts_seq", "static_data")
            .StartsAt(100000);
			
        modelBuilder.Entity<Contract>().HasIndex(a => a.Ticker).IsUnique();
    }
}