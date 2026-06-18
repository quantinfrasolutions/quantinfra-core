using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Databases.Main.Models.StaticData
{
    public class StreamConfiguration : IEntityTypeConfiguration<Stream>
    {
        public void Configure(EntityTypeBuilder<Stream> builder)
        {
            builder.ToTable("streams", "static_data");
            builder.HasKey(a => a.StreamId);
            builder.Property(a => a.StreamId).HasColumnName("stream_id")
                .HasDefaultValueSql("nextval('static_data.streams_seq')")
                .IsRequired();
            builder.Property(a => a.Ticker).HasColumnName("ticker");
            // builder.Property(a => a.ExchangeId).HasColumnName("exchange_id");
            builder.Property(a => a.DatafeedId).HasColumnName("datafeed_id");
            // builder.Property(a => a.Enabled).HasColumnName("enabled");
            builder.HasOne(a => a.Contract)
                .WithMany(c => c.Streams)
                .HasForeignKey("contract_id")
                .OnDelete(DeleteBehavior.Restrict);
        }


        public static void CreateRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("streams_seq", "static_data")
                .StartsAt(1000000);
        }
    }
}

