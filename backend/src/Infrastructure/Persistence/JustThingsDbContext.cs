using Domain.Catalogue;
using Domain.Colours;
using Domain.Ordering;
using Domain.Pricing;
using Domain.Sellers;
using Domain.Verticals;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// The commerce database. Bounded contexts share one context and one transaction boundary --
/// a modular monolith, per docs/architecture.md. Splitting later is cheaper than distributed
/// transactions now.
/// </summary>
public class JustThingsDbContext(DbContextOptions<JustThingsDbContext> options) : DbContext(options)
{
    public DbSet<Vertical> Verticals => Set<Vertical>();

    public DbSet<Seller> Sellers => Set<Seller>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ColourSystem> ColourSystems => Set<ColourSystem>();

    public DbSet<Colour> Colours => Set<Colour>();

    public DbSet<TintBase> TintBases => Set<TintBase>();

    public DbSet<ColourAvailability> ColourAvailabilities => Set<ColourAvailability>();

    public DbSet<PricingTier> PricingTiers => Set<PricingTier>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartLine> CartLines => Set<CartLine>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JustThingsDbContext).Assembly);
    }
}
