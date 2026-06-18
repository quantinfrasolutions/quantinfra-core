using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("contract_templates", "static_data");
        builder.HasKey(a => a.TemplateId);
        builder.Property(a => a.TemplateId).HasColumnName("template_id")
            .HasDefaultValueSql("nextval('static_data.contract_templates_seq')")
            .IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.SecurityType).HasColumnName("security_type").HasColumnType("text").IsRequired();
        builder.Property(t => t.PlCalculatorType).HasColumnName("pl_calculator_type").HasColumnType("text").IsRequired();
        builder.HasOne(t => t.Asset)
            .WithMany()
            .HasForeignKey("asset_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(t => t.MinSize).HasColumnName("min_size").IsRequired();
        builder.Property(t => t.MinSizeMoney).HasColumnName("min_size_money");
        builder.Property(t => t.MaxSize).HasColumnName("max_size").IsRequired();
        builder.Property(t => t.MaxSizeMoney).HasColumnName("max_size_money");
        builder.Property(t => t.SizeIncrement).HasColumnName("size_increment").IsRequired();
        builder.Property(t => t.TickSize).HasColumnName("tick_size").IsRequired();
        builder.Property(t => t.TickValue).HasColumnName("tick_value").IsRequired();
        builder.Property(t => t.PriceQuotation).HasColumnName("price_quotation").IsRequired();
        builder.HasOne(t => t.SettlementCurrency)
            .WithMany()
            .HasForeignKey("settlement_currency_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.BaseCurrency)
            .WithMany()
            .HasForeignKey("base_currency_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.QuoteCurrency)
            .WithMany()
            .HasForeignKey("quote_currency_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.DefaultDatafeed)
            .WithMany()
            .HasForeignKey("default_datafeed_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.TradingSessions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "contract_template_trading_sessions",   // join table name
                j => j
                    .HasOne<TradingSession>()
                    .WithMany()
                    .HasForeignKey("trading_session_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<ContractTemplate>()
                    .WithMany()
                    .HasForeignKey("contract_template_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("contract_templates_trading_sessions");
                    j.HasKey("contract_template_id", "trading_session_id");
                    j.HasIndex("trading_session_id");
                });
        builder.HasMany(x => x.Commissions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "contract_templates_commissions",   // join table name
                j => j
                    .HasOne<CommissionStructure>()
                    .WithMany()
                    .HasForeignKey("commission_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<ContractTemplate>()
                    .WithMany()
                    .HasForeignKey("contract_template_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("contract_templates_commissions");
                    j.HasKey("contract_template_id", "commission_id");
                    j.HasIndex("commission_id");
                });
        builder.HasOne(t => t.Exchange)
            .WithMany()
            .HasForeignKey("exchange_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Broker)
            .WithMany()
            .HasForeignKey("broker_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(t => t.DaysInYear).HasColumnName("days_in_year").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
    }

    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("contract_templates_seq", "static_data")
            .StartsAt(10000);

        modelBuilder.Entity<ContractTemplate>().HasIndex(a => a.Name).IsUnique();
    }
}