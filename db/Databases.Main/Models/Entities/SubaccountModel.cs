using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.Entities;

public class SubaccountModel : Subaccount
{
    public SubaccountModel() { }

    public SubaccountModel(Subaccount other) : base(other)
    { }
    
    public AccountModel Account { get; init; }
    public AccountModel Subaccount { get; init; }
    public Broker? Broker { get; init; }
    public bool IsActive { get; init; } = true;
}

public class SubaccountConfiguration : IEntityTypeConfiguration<SubaccountModel>
{
    public void Configure(EntityTypeBuilder<SubaccountModel> builder)
    {
        builder.ToTable("subaccounts", "entities");
        builder.HasKey(a => a.SubaccountHistoryId);
        builder.Property(a => a.SubaccountHistoryId).HasColumnName("subaccount_history_id").IsRequired()
            .HasDefaultValueSql("nextval('entities.subaccounts_seq')");
        builder.Property(a => a.AccountId).HasColumnName("account_id").IsRequired();
        builder.HasOne(a => a.Account)
            .WithMany(a => a.Subaccounts)
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.SubaccountId).HasColumnName("subaccount_id").IsRequired();
        builder.HasOne(a => a.Subaccount)
            .WithMany()
            .HasForeignKey(a => a.SubaccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.Classifier).HasColumnName("classifier").HasColumnType("text").IsRequired();
        builder.Property(a => a.BrokerId).HasColumnName("broker_id");
        builder.HasOne(a => a.Broker)
            .WithMany()
            .HasForeignKey(a => a.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.IsActive).HasColumnName("is_active").IsRequired();
    }
    
    public static void CreateRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("subaccounts_seq", "entities")
            .StartsAt(1000000);
    }
}