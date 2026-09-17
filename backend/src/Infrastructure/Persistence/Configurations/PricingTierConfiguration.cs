using Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PricingTierConfiguration : IEntityTypeConfiguration<PricingTier>
{
    public void Configure(EntityTypeBuilder<PricingTier> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(60).IsRequired();
        builder.Property(t => t.Multiplier).HasPrecision(6, 4);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
