using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantInfra.Sdk.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Databases.Main.Models.StaticData;

public class ConstantStreamConfiguration : IEntityTypeConfiguration<ConstantStreamValue>
{
    public void Configure(EntityTypeBuilder<ConstantStreamValue> builder)
    {
        builder.ToTable("constant_value_streams", "static_data");
        builder.HasKey(a => a.StreamId);
        builder.Property(a => a.StreamId).HasColumnName("stream_id").IsRequired();
        builder.Property(a => a.Value).HasColumnName("value").IsRequired();
        builder.Ignore(a => a.CronExpression);
        builder.HasOne<Stream>()
            .WithOne(s => s.ConstantStreamValue)
            .HasForeignKey<ConstantStreamValue>(a => a.StreamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}