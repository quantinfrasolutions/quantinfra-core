using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Infrastructure;
using TradingClientConfig = QuantInfra.Sdk.Accounts.TradingClientConfig;

namespace QuantInfra.Databases.Main.Models.Entities;

public class TradingClientConfigConfiguration : IEntityTypeConfiguration<TradingClientConfig>
{
    public void Configure(EntityTypeBuilder<TradingClientConfig> builder)
    {
        builder.ToTable("trading_clients", "entities");
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.ExecutionServiceName).HasColumnName("execution_service").IsRequired();
        builder.Property(a => a.ExternalAccountId).HasColumnName("external_account_id");
        builder.Property(a => a.TradingClientClassName).HasColumnName("class_name").IsRequired();
        builder.Property(a => a.TradingClientParamsSerialized).HasColumnName("params").IsRequired();
        builder.Property(a => a.TradingClientSecret).HasColumnName("secret").IsRequired();
        builder.Ignore(a => a.WritePerformanceMetrics);
		
        builder.HasOne<AccountModel>()
            .WithOne(a => a.TradingClientConfig)
            .HasForeignKey<TradingClientConfig>(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<ExecutionServiceInstanceModel>()
            .WithMany(es => es.TradingClients)
            .HasForeignKey(t => t.ExecutionServiceName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}