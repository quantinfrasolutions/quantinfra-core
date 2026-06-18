using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class CommissionStructureConfiguration : IEntityTypeConfiguration<CommissionStructure>
{
	public void Configure(EntityTypeBuilder<CommissionStructure> builder)
	{
		builder.ToTable("commissions", "static_data");
		builder.HasKey(a => a.CommissionId);
		builder.Property(a => a.CommissionId).HasColumnName("commission_id")
			.HasDefaultValueSql("nextval('static_data.commissions_seq')")
			.IsRequired();
		builder.Property(a => a.Name).HasColumnName("name").IsRequired();
		builder.Property(a => a.Description).HasColumnName("description");
		builder.Property(a => a.FixedPerShare).HasColumnName("fixed_per_share").IsRequired();
		builder.Property(a => a.Floating).HasColumnName("floating").IsRequired();
		builder.Property(a => a.CommissionStructureType).HasColumnName("commission_structure_type")
			.HasColumnType("text").IsRequired();
		builder.HasOne(a => a.Currency)
			.WithMany()
			.HasForeignKey("currency_id")
			.OnDelete(DeleteBehavior.Restrict);
		builder.Property(a => a.BrokerId).HasColumnName("broker_id");
		builder.HasOne<Broker>()
			.WithMany()
			.HasForeignKey(a => a.BrokerId)
			.OnDelete(DeleteBehavior.Restrict);
		builder.Property(a => a.ExchangeId).HasColumnName("exchange_id");
		builder.HasOne<Exchange>()
			.WithMany()
			.HasForeignKey(a => a.ExchangeId)
			.OnDelete(DeleteBehavior.Restrict);
	}

	public static void CreateRelations(ModelBuilder modelBuilder)
	{
		modelBuilder.HasSequence<int>("commissions_seq", "static_data")
			.StartsAt(1000);
	}
}