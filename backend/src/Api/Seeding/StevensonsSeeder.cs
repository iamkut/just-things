using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Catalogue;
using Domain.Colours;
using Domain.Pricing;
using Domain.Sellers;
using Domain.Verticals;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Seeding;

/// <summary>
/// Seeds the Just Paints vertical with Stevensons as its first seller.
/// </summary>
/// <remarks>
/// Colour names, codes and hex values come from Stevensons' own published Real Colours
/// swatches. LRV is derived from hex; hue family and tint-base assignment are DERIVED
/// HEURISTICS standing in until Stevensons supplies the real base mapping, which is the
/// open Phase 0 ask. Product prices, pack sizes and weights are INVENTED placeholders.
/// Nothing here should be quoted commercially.
/// </remarks>
public sealed class StevensonsSeeder(JustThingsDbContext db, ILogger<StevensonsSeeder> logger)
{
    private sealed record SeedColour(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("hex")] string Hex,
        [property: JsonPropertyName("lrv")] decimal Lrv,
        [property: JsonPropertyName("family")] string Family,
        [property: JsonPropertyName("tintBase")] string TintBase);

    private static readonly (string Name, decimal UpliftPerLitre, int Sort)[] Bases =
    [
        ("White base", 0m, 0),
        ("Pastel base", 6m, 1),
        ("Medium base", 14m, 2),
        ("Deep base", 26m, 3),
        ("Accent base", 38m, 4),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Verticals.AnyAsync(ct))
        {
            logger.LogInformation("Seed skipped: data already present.");
            return;
        }

        var vertical = new Vertical
        {
            Id = Guid.CreateVersion7(),
            Name = "Just Paints",
            Slug = "paints",
            Hostname = "paints.justthings.co.za",
        };
        db.Verticals.Add(vertical);

        var seller = new Seller
        {
            Id = Guid.CreateVersion7(),
            LegalName = "Stevensons Paint (Pty) Ltd",
            DisplayName = "Stevensons",
            Slug = "stevensons",
            CommissionRate = 0.25m,
            DefaultFulfilmentMode = FulfilmentMode.SellerDropship,
        };
        db.Sellers.Add(seller);

        db.PricingTiers.Add(new PricingTier
        {
            Id = Guid.CreateVersion7(), Name = "Retail", Multiplier = 1m, IsDefault = true,
        });

        var tintBases = Bases.Select(b => new TintBase
        {
            Id = Guid.CreateVersion7(),
            SellerId = seller.Id,
            Name = b.Name,
            UpliftPerLitreExVat = b.UpliftPerLitre,
            SortOrder = b.Sort,
        }).ToDictionary(t => t.Name);
        db.TintBases.AddRange(tintBases.Values);

        var system = new ColourSystem
        {
            Id = Guid.CreateVersion7(),
            Name = "Stevensons Real Colours",
            Slug = "stevensons-real-colours",
            OwnerSellerId = seller.Id,
            IsProprietary = true,
        };
        db.ColourSystems.Add(system);

        var colours = await LoadColoursAsync(ct);
        var colourEntities = new List<(Colour Colour, string BaseName)>(colours.Count);

        foreach (var (seed, index) in colours.Select((c, i) => (c, i)))
        {
            var colour = new Colour
            {
                Id = Guid.CreateVersion7(),
                ColourSystemId = system.Id,
                Code = seed.Code,
                Name = seed.Name,
                Hex = seed.Hex,
                Lrv = seed.Lrv,
                HueFamily = Enum.TryParse<HueFamily>(seed.Family, true, out var family)
                    ? family
                    : HueFamily.Neutral,
                SortOrder = index,
            };
            colourEntities.Add((colour, seed.TintBase + " base"));
        }

        db.Colours.AddRange(colourEntities.Select(c => c.Colour));

        var brand = new Brand
        {
            Id = Guid.CreateVersion7(), SellerId = seller.Id, Name = "Stevensons", Slug = "stevensons",
        };
        db.Brands.Add(brand);

        var category = new Category
        {
            Id = Guid.CreateVersion7(),
            VerticalId = vertical.Id,
            Name = "Interior walls",
            Slug = "interior-walls",
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            VerticalId = vertical.Id,
            BrandId = brand.Id,
            CategoryId = category.Id,
            Name = "Architect Premium Interior",
            Slug = "architect-premium-interior",
            Description = "Low-VOC acrylic topcoat for interior walls and ceilings.",
            CoveragePerLitre = 8m,
            CoatsRecommended = 2,
            IsTintable = true,
        };
        db.Products.Add(product);

        // Placeholder prices. Sheen uplift: matt is the reference, silk +4%, eggshell +6%.
        (Sheen Sheen, decimal Uplift)[] sheens =
        [
            (Sheen.Matt, 0m),
            (Sheen.Silk, 0.04m),
            (Sheen.Eggshell, 0.06m),
        ];
        (decimal Litres, decimal PriceExVat, decimal WeightKg)[] packs =
        [
            (1m, 189.00m, 1.4m),
            (5m, 749.00m, 6.6m),
            (20m, 2609.00m, 25.4m),
        ];

        foreach (var sheen in sheens)
        {
            foreach (var pack in packs)
            {
                var variant = new ProductVariant
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = product.Id,
                    Sku = $"STV-ARCH-{sheen.Sheen.ToString().ToUpperInvariant()}-{pack.Litres:0}L",
                    Sheen = sheen.Sheen,
                    PackLitres = pack.Litres,
                    WeightKg = pack.WeightKg,
                    SheenUpliftFactor = sheen.Uplift,
                    HazardClass = HazardClass.None,
                };
                db.ProductVariants.Add(variant);

                db.Offers.Add(new Offer
                {
                    Id = Guid.CreateVersion7(),
                    SellerId = seller.Id,
                    ProductVariantId = variant.Id,
                    PriceExVat = pack.PriceExVat,
                    StockQuantity = 100,
                    LeadTimeDays = 2,
                    FulfilmentMode = FulfilmentMode.SellerDropship,
                });
            }
        }

        // Every Real Colour is tintable in this range, each resolving to one base. This is the
        // join that makes runtime pricing possible -- and the mapping Stevensons must confirm.
        db.ColourAvailabilities.AddRange(colourEntities.Select(c => new ColourAvailability
        {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            ColourId = c.Colour.Id,
            TintBaseId = tintBases[c.BaseName].Id,
            ColourantSurchargeExVat = 0m,
        }));

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded {Colours} colours, {Variants} variants and {Availabilities} colour-availability rows.",
            colourEntities.Count, sheens.Length * packs.Length, colourEntities.Count);
    }

    private static async Task<IReadOnlyList<SeedColour>> LoadColoursAsync(CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SeedData", "stevensons-real-colours.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Colour seed data is missing.", path);
        }

        await using var stream = File.OpenRead(path);
        var colours = await JsonSerializer.DeserializeAsync<List<SeedColour>>(stream, cancellationToken: ct);

        return colours ?? throw new InvalidOperationException("Colour seed data could not be read.");
    }
}
