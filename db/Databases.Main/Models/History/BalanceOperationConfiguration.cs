using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.History;

public class BalanceOperationConfiguration : IEntityTypeConfiguration<BalanceOperation>
{
    public void Configure(EntityTypeBuilder<BalanceOperation> builder)
    {
        builder.ToTable("balance_operations", "history");
        
        builder.Property(a => a.AccountServiceName).HasColumnName("account_service_name").IsRequired();
        builder.Property(a => a.BalanceOperationId).HasColumnName("balance_operation_id").IsRequired();
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(a => a.Dt).HasColumnName("dt").IsRequired();
        builder.Property(a => a.Amount).HasColumnName("amount").IsRequired();
        builder.Property(a => a.AssetId).HasColumnName("asset_id").IsRequired();
        builder.Property(a => a.Price).HasColumnName("price").IsRequired();
        builder.Property(a => a.FxRate).HasColumnName("fx_rate").IsRequired();
        builder.Property(a => a.ValueInAccountCcy).HasColumnName("value_in_account_ccy").IsRequired();
        builder.Property(a => a.ExternalId).HasColumnName("external_id");
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.IsCorrection).HasColumnName("is_correction").IsRequired();
        builder.Property(a => a.AffectsPnL).HasColumnName("affects_pnl").IsRequired();
        builder.Property(a => a.AffectsInvestment).HasColumnName("affects_investment").IsRequired();
        builder.Property(a => a.AffectsBalance).HasColumnName("affects_balance").IsRequired();
        builder.Property(a => a.AffectsShareCount).HasColumnName("affects_share_count").IsRequired();
        
        // Balance operation id is unique only within one AS instance
        builder.HasKey(bo => new { bo.AccountServiceName, bo.BalanceOperationId });
        
        builder.HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<Event>()
            .WithOne(e => e.BalanceOperation)
            .HasForeignKey<Event>(e => new { e.AccountServiceName, e.BalanceOperationId })
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(a => new { a.AccountId, a.ExternalId }).AreNullsDistinct(true).IsUnique(true);
    }
}