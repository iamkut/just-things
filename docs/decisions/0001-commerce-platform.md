# ADR-0001: Commerce platform

**Status:** Accepted
**Date:** 2026-09-17

## Context

Just Paints needs a commerce engine. The candidates are a custom .NET 10 core,
a headless open-source engine (Medusa v2, Saleor, Vendure), or Shopify Plus.

The selection criterion is not feature count. It is how well the platform
tolerates **runtime price resolution**, because paint pricing depends on a tint
base derived from the chosen colour (see ADR-0002).

## Decision

**Build the commerce core on .NET 10 and integrate everything else.**
Accepted 2026-09-17.

Shopify Plus is eliminated. It raised the variant ceiling to 2,048 in October
2025 but still permits only 3 options per product, and Liquid caps
`product.variants` at 250. Paint needs colour, base, sheen and pack size, and
Shopify cannot price a line item from a runtime rule without dropping to draft
orders — which breaks the storefront checkout.

Medusa v2 was the considered alternative and was not taken.

## Consequences

**Accepting:**
- Roughly two extra months before the first real transaction
- Cart, checkout, orders, tax and returns are ours to build and maintain

**Gaining:**
- The differentiating code — colour, tinting, pricing, coverage — is first-class
  rather than fighting a framework's grain
- No second runtime to host, patch and upgrade; the team already runs .NET 10
  Clean Architecture on other projects
- Marketplace and trade-account work in Phases 2 and 3 stay tractable

**Not building:** payments (Peach/Stitch), shipping (Bob Go), search
(Meilisearch), media, transactional email. These are integrations.

## Database

PostgreSQL, via Npgsql and EF Core 10, with snake_case naming to match the xcii
convention. Chosen over SQL Server on hosting cost for a consumer marketplace.

The provider is confined to `Infrastructure/DependencyInjection.cs` and the
design-time factory. Switching to SQL Server is those two call sites, a package
swap and a regenerated migration.

## Revisit if

Runway tightens such that two months matters more than five years of
maintainability. Reversing this after Phase 1 takes money means rebuilding a
working checkout, so the window for changing course is now, not later.
