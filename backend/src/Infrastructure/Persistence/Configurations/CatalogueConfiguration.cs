using Domain.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(120).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(60).IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(60).IsRequired();
        builder.HasIndex(c => new { c.VerticalId, c.Slug }).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.CoveragePerLitre).HasPrecision(6, 2);

        builder.HasMany(p => p.Variants).WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.VerticalId, p.Slug }).IsUnique();
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.Sku).HasMaxLength(60).IsRequired();
        builder.Property(v => v.Sheen).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.HazardClass).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.PackLitres).HasPrecision(9, 3);
        builder.Property(v => v.WeightKg).HasPrecision(9, 3);
        builder.Property(v => v.SheenUpliftFactor).HasPrecision(6, 4);

        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => new { v.ProductId, v.Sheen, v.PackLitres }).IsUnique();
    }
}
