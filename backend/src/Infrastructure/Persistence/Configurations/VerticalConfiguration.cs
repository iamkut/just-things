using Domain.Verticals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class VerticalConfiguration : IEntityTypeConfiguration<Vertical>
{
    public void Configure(EntityTypeBuilder<Vertical> builder)
    {
        builder.Property(v => v.Name).HasMaxLength(120).IsRequired();
        builder.Property(v => v.Slug).HasMaxLength(60).IsRequired();
        builder.Property(v => v.Hostname).HasMaxLength(200).IsRequired();

        builder.HasIndex(v => v.Slug).IsUnique();
        builder.HasIndex(v => v.Hostname).IsUnique();
    }
}
