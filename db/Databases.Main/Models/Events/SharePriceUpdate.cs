using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Infrastructure;

namespace QuantInfra.Databases.Main.Models.Events;

[Table("share_price_updates", Schema = "events")]
public class SharePriceUpdate
{
    [Column("account_service_name")] public string AccountServiceName { get; init; }
    
    [Column("event_id")] public long EventId { get; init; }
    public Event Event { get; init; }
    
    [Column("account_id")] public int AccountId { get; init; }
    [Column("equity")] public decimal Equity { get; init; }
    [Column("share_price")] public decimal SharePrice { get; init; }
    [Column("daily_return")] public decimal DailyReturn { get; init; }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharePriceUpdate>().HasKey(x => new { x.AccountServiceName, x.EventId });
        
        modelBuilder.Entity<SharePriceUpdate>()
            .HasOne<AccountServiceInstanceModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<SharePriceUpdate>()
            .HasOne(s => s.Event)
            .WithOne(e => e.SharePriceUpdate)
            .HasForeignKey<SharePriceUpdate>(x => new { x.AccountServiceName, x.EventId })
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<SharePriceUpdate>()
            .HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}