# ADR-0001: Commerce platform

**Status:** Proposed — awaiting decision
**Date:** 2026-09-17

## Context

Just Paints needs a commerce engine. The candidates are a custom .NET 10 core,
a headless open-source engine (Medusa v2, Saleor, Vendure), or Shopify Plus.

The selection criterion is not feature count. It is how well the platform
tolerates **runtime price resolution**, because paint pricing depends on a tint
base derived from the chosen colour (see ADR-0002).

## Decision

Recommended: **build the commerce core on .NET 10, integrate everything else.**

Shopify Plus is eliminated. It raised the variant ceiling to 2,048 in October
2025 but still permits only 3 options per product, and Liquid caps
`product.variants` at 250. Paint needs colour, base, sheen and pack size, and
Shopify cannot price a line item from a runtime rule without dropping to draft
orders — which breaks the storefront checkout.

Medusa v2 is the fallback if time-to-first-revenue outranks architectural fit.

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

## Revisit if

Runway tightens such that two months matters more than five years of
maintainability. In that case take Medusa v2, keep ADR-0002, and accept harder
Phase 3 work.
