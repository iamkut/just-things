using Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.Property(c => c.AnonymousId).HasMaxLength(100);
        builder.Ignore(c => c.TotalIncVat);

        builder.HasMany(c => c.Lines).WithOne()
            .HasForeignKey(l => l.CartId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.AnonymousId);
        builder.HasIndex(c => c.CustomerId);
    }
}

public class CartLineConfiguration : IEntityTypeConfiguration<CartLine>
{
    public void Configure(EntityTypeBuilder<CartLine> builder)
    {
        builder.Ignore(l => l.LineTotalIncVat);

        builder.HasOne(l => l.Offer).WithMany()
            .HasForeignKey(l => l.OfferId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(l => l.Configuration, ConfigureLineConfiguration);
    }

    internal static void ConfigureLineConfiguration<T>(OwnedNavigationBuilder<T, LineConfiguration> cfg)
        where T : class
    {
        cfg.Property(c => c.ColourCode).HasMaxLength(40);
        cfg.Property(c => c.ColourName).HasMaxLength(120);
        cfg.Property(c => c.ColourHex).HasMaxLength(7);
        cfg.Property(c => c.TintBaseName).HasMaxLength(60);
        cfg.Property(c => c.ResolvedPriceExVat).HasPrecision(18, 2);
        cfg.Property(c => c.ResolvedPriceIncVat).HasPrecision(18, 2);
        cfg.Property(c => c.VatFraction).HasPrecision(6, 4);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.ReferenceNumber).HasMaxLength(40).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.VatFraction).HasPrecision(6, 4);
        builder.Property(o => o.SubtotalIncVat).HasPrecision(18, 2);
        builder.Property(o => o.ShippingIncVat).HasPrecision(18, 2);
        builder.Property(o => o.TotalIncVat).HasPrecision(18, 2);
        builder.Ignore(o => o.HasMadeToOrderLines);

        builder.HasMany(o => o.Lines).WithOne()
            .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.ReferenceNumber).IsUnique();
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Sku).HasMaxLength(60).IsRequired();
        builder.Property(l => l.SheenName).HasMaxLength(20);
        builder.Property(l => l.PackLitres).HasPrecision(9, 3);
        builder.Property(l => l.WeightKg).HasPrecision(9, 3);

        // Fixed at placement and never recomputed. See ADR-0002.
        builder.Property(l => l.Returnability).HasConversion<string>().HasMaxLength(40);
        builder.Ignore(l => l.LineTotalIncVat);

        builder.OwnsOne(l => l.Configuration, CartLineConfiguration.ConfigureLineConfiguration);
    }
}
