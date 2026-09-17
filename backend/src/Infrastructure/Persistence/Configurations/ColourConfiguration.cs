using Domain.Colours;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ColourSystemConfiguration : IEntityTypeConfiguration<ColourSystem>
{
    public void Configure(EntityTypeBuilder<ColourSystem> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(60).IsRequired();

        builder.HasMany(c => c.Colours).WithOne(c => c.ColourSystem)
            .HasForeignKey(c => c.ColourSystemId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

public class ColourConfiguration : IEntityTypeConfiguration<Colour>
{
    public void Configure(EntityTypeBuilder<Colour> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();

        // Nullable on purpose: a manufacturer may publish names and codes long before it
        // shares colour values, and the catalogue has to sell before the data is complete.
        builder.Property(c => c.Hex).HasMaxLength(7);
        builder.Property(c => c.Lrv).HasPrecision(5, 2);
        builder.Property(c => c.HueFamily).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => new { c.ColourSystemId, c.Code }).IsUnique();
        builder.HasIndex(c => c.HueFamily);
    }
}

public class TintBaseConfiguration : IEntityTypeConfiguration<TintBase>
{
    public void Configure(EntityTypeBuilder<TintBase> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(60).IsRequired();
        builder.Property(t => t.UpliftPerLitreExVat).HasPrecision(18, 2);

        builder.HasIndex(t => new { t.SellerId, t.Name }).IsUnique();
    }
}

public class ColourAvailabilityConfiguration : IEntityTypeConfiguration<ColourAvailability>
{
    public void Configure(EntityTypeBuilder<ColourAvailability> builder)
    {
        builder.Property(a => a.ColourantSurchargeExVat).HasPrecision(18, 2);

        builder.HasOne(a => a.Colour).WithMany()
            .HasForeignKey(a => a.ColourId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.TintBase).WithMany()
            .HasForeignKey(a => a.TintBaseId).OnDelete(DeleteBehavior.Restrict);

        // A product resolves a given colour to exactly one base.
        builder.HasIndex(a => new { a.ProductId, a.ColourId }).IsUnique();
    }
}
