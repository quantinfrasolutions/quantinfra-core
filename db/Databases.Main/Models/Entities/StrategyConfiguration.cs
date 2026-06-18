using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Databases.Main.Models.Entities;

[Table("strategies", Schema = "entities")]
public class StrategyConfiguration : IEntityTypeConfiguration<StrategyModel> 
{
    public void Configure(EntityTypeBuilder<StrategyModel> builder)
    {
        builder.ToTable("strategies", "entities");
        builder.HasKey(a => a.StrategyId);
        builder.Property(a => a.StrategyId).HasColumnName("strategy_id")
            .HasDefaultValueSql("nextval('entities.strategies_seq')")
            .IsRequired();
        builder.Property(a => a.StrategyServiceName).HasColumnName("strategies_service");
        builder.HasOne<StrategiesServiceInstanceModel>()
            .WithMany(ss => ss.Strategies)
            .HasForeignKey(a => a.StrategyServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.HasOne(s => s.Account)
            .WithOne(a => a.Strategy)
            .HasForeignKey<StrategyModel>(s => s.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.ClassName).HasColumnName("class_name").IsRequired();
        builder.Property(s => s.UseSignalGroups).HasColumnName("use_signal_groups").IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasColumnType("text").IsRequired();
        builder.Property(s => s.Params).HasColumnName("params").IsRequired();
        builder.Property(s => s.RequiredBarStorages).HasColumnName("required_bar_storages")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.Symbols).HasColumnName("symbols")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.LiquidationParameters).HasColumnName("liquidation_parameters")
            .HasColumnType("jsonb");
    }

    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Strategy>();
        // modelBuilder.Entity<Strategy>().HasKey(a => a.StrategyId);
        
        modelBuilder.HasSequence<int>("strategies_seq", "entities")
            .StartsAt(50000);
        
        modelBuilder.Entity<StrategyModel>().HasIndex(a => a.Name).IsUnique();
    }
}