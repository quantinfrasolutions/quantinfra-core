using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.History;

public class BalanceValueModel : BalanceValue
{
    public BalanceValueModel(
        string accountServiceName,
        long? eventId,
        int accountId,
        int currencyId,
        Instant dt,
        decimal cashBalance,
        decimal holdings,
        decimal unrealizedPnL,
        decimal futuresVariationMargin,
        decimal totalBalance,
        decimal totalValue,
        decimal fxRate
    ) : base(accountId, currencyId, dt, cashBalance, holdings, unrealizedPnL, futuresVariationMargin, totalBalance, totalValue, fxRate)
    {
        AccountServiceName = accountServiceName;
        EventId = eventId;
    }
    
    public BalanceValueModel(string accountServiceName, long? eventId, BalanceValue v) : this(accountServiceName, eventId,
        v.AccountId, v.CurrencyId, v.Dt, v.CashBalance, v.Holdings, v.UnrealizedPnL, v.FuturesVariationMargin, 
        v.TotalBalance, v.TotalValue, v.FxRate)
    { }
    
    public string AccountServiceName { get; init; }
    public long? EventId { get; init; }
    
    public Currency Currency { get; init; }
}

public class EndOfDayBalanceConfiguration : IEntityTypeConfiguration<BalanceValueModel>
{
    public void Configure(EntityTypeBuilder<BalanceValueModel> builder)
    {
        builder.ToTable("end_of_day_balances", "history");
        
        builder.HasKey(x => new { x.AccountServiceName, x.AccountId, x.CurrencyId, x.Dt });
        builder.Property(x => x.AccountServiceName).HasColumnName("as_name").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.CurrencyId).HasColumnName("currency_id").IsRequired();
        builder.Property(x => x.Dt).HasColumnName("dt").IsRequired();
        builder.Property(x => x.CashBalance).HasColumnName("cash_balance").IsRequired();
        builder.Property(x => x.Holdings).HasColumnName("holdings").IsRequired();
        builder.Property(x => x.UnrealizedPnL).HasColumnName("unrealized_pnl").IsRequired();
        builder.Property(x => x.FuturesVariationMargin).HasColumnName("futures_vm").IsRequired();
        builder.Property(x => x.TotalBalance).HasColumnName("total_balance").IsRequired();
        builder.Property(x => x.TotalValue).HasColumnName("total_value").IsRequired();
        builder.Property(x => x.FxRate).HasColumnName("fx_rate").IsRequired();
        builder.Property(x => x.EventId).HasColumnName("event_id");
        
        builder.HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<AccountServiceInstanceModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(b => b.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne<Event>()
            .WithMany(e => e.EndOfDayBalances)
            .HasForeignKey(b => new { b.AccountServiceName, b.EventId })
            .OnDelete(DeleteBehavior.SetNull);
    }
}