using Ambev.DeveloperEvaluation.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public sealed class SaleIdempotencyRecordConfiguration : IEntityTypeConfiguration<SaleIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<SaleIdempotencyRecord> builder)
    {
        builder.ToTable("SaleIdempotencyRecords");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.Property(x => x.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.SaleId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<Sale>()
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
