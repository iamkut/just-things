using Domain.Sellers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.Property(o => o.PriceExVat).HasPrecision(18, 2);
        builder.Property(o => o.FulfilmentMode).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(o => o.Seller).WithMany()
            .HasForeignKey(o => o.SellerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ProductVariant).WithMany()
            .HasForeignKey(o => o.ProductVariantId).OnDelete(DeleteBehavior.Cascade);

        // One seller lists a given variant once. Two sellers listing the same variant is the
        // buy-box case and is expected -- see ADR-0004.
        builder.HasIndex(o => new { o.SellerId, o.ProductVariantId }).IsUnique();
    }
}
