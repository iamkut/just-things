using Domain.Sellers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.Property(s => s.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Slug).HasMaxLength(60).IsRequired();
        builder.Property(s => s.CompanyRegistrationNumber).HasMaxLength(40);
        builder.Property(s => s.VatNumber).HasMaxLength(20);
        builder.Property(s => s.CommissionRate).HasPrecision(6, 4);
        builder.Property(s => s.DefaultFulfilmentMode).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(s => s.Slug).IsUnique();
    }
}
