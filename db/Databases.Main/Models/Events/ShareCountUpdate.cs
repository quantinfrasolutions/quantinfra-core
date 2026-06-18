using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Databases.Main.Models.Events;

[Table("share_count_updates", Schema = "events")]
public class ShareCountUpdate
{
    [Column("account_service_name"), Required] public string AccountServiceName { get; set; }
    [Column("event_id"), Required] public long EventId { get; init; }
    public Event Event { get; set; }
    [Column("account_id"), Required] public int AccountId { get; init; }
    [Column("change"), Required] public decimal Change { get; init; }
    [Column("bo_id"), Required] public int BalanceOperationId { get; init; }

    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShareCountUpdate>().HasKey(x => new { x.AccountServiceName, x.EventId });
        
        modelBuilder.Entity<ShareCountUpdate>()
            .HasOne<AccountServiceInstanceModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountServiceName)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ShareCountUpdate>()
            .HasOne(s => s.Event)
            .WithOne(e => e.ShareCountUpdate)
            .HasForeignKey<ShareCountUpdate>(x => new { x.AccountServiceName, x.EventId })
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ShareCountUpdate>()
            .HasOne<AccountModel>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ShareCountUpdate>()
            .HasOne<BalanceOperation>()
            .WithMany()
            .HasForeignKey(a => new { a.AccountServiceName, a.BalanceOperationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}