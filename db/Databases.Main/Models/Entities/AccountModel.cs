using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.Entities;

public class AccountModel : AccountRecordV6
{
    public AccountModel() { }

    public AccountModel(string accountServiceName, string name, int currencyId, AccountType accountType,
        PositionAccounting positionAccounting, int? brokerId, bool enableSharePriceTracking, bool includeUnrealizedPnLToMtm,
        TradingClientConfig? tradingClientConfig, Broker? broker
    ) : base(accountServiceName, name, currencyId, accountType, positionAccounting, brokerId, enableSharePriceTracking,
        includeUnrealizedPnLToMtm, tradingClientConfig
    )
    { }

    public Currency Currency { get; init; }
    public StrategyModel? Strategy { get; init; }
    public ICollection<SubaccountModel> Subaccounts { get; init; }
    public Broker? Broker { get; init; }
}

public class AccountConfiguration : IEntityTypeConfiguration<AccountModel>
{
    public void Configure(EntityTypeBuilder<AccountModel> builder)
    {
        builder.ToTable("accounts", "entities");
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.AccountId).HasColumnName("account_id")
            .HasDefaultValueSql("nextval('entities.accounts_seq')")
            .IsRequired();
        builder.Property(a => a.AccountServiceName).HasColumnName("account_service").IsRequired();
        builder.HasOne<AccountServiceInstanceModel>()
            .WithMany(s => s.Accounts)
            .HasForeignKey(a => a.AccountServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.CurrencyId).HasColumnName("currency_id").IsRequired();
        builder.HasOne(a => a.Currency)
            .WithMany()
            .HasForeignKey(a => a.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.AccountType).HasColumnName("account_type").HasColumnType("text").IsRequired();
        builder.Property(a => a.PositionAccounting).HasColumnName("position_accounting").HasColumnType("text").IsRequired();
        builder.Property(a => a.BrokerId).HasColumnName("broker_id");
        builder.HasOne(a => a.Broker)
            .WithMany()
            .HasForeignKey(a => a.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.EnableSharePriceTracking).HasColumnName("enable_share_price_tracking").IsRequired();
        builder.Property(a => a.IncludeUnrealizedPnLToMtm).HasColumnName("include_unrealized_pnl_to_mtm").IsRequired();
    }
    
    


    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<AccountRecordV6>();
        // modelBuilder.Entity<AccountRecordV6>().HasKey(a => a.AccountId);
        
        modelBuilder.HasSequence<int>("accounts_seq", "entities")
            .StartsAt(1000000);
        
        modelBuilder.Entity<AccountModel>().HasIndex(a => a.Name).IsUnique();
    }
}