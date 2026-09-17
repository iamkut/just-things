# Architecture

Status: **accepted**. The commerce platform decision was taken on 2026-09-17 —
custom .NET 10 core, see `decisions/0001-commerce-platform.md`. The backend lives
in `backend/`.

## Shape

A single platform serving multiple vertical storefronts, launching with one.

```mermaid
flowchart TB
    subgraph Storefronts
        P[paints.justthings.co.za]
        T[justthings.co.za<br/>Phase 4]
    end
    subgraph Platform
        API[Commerce API<br/>.NET 10]
        ADM[Admin / Seller portal]
    end
    subgraph External
        PAY[Peach / Stitch]
        SHIP[Bob Go]
        SRCH[Meilisearch]
    end
    P --> API
    T --> API
    ADM --> API
    API --> PAY
    API --> SHIP
    API --> SRCH
```

Verticals are data, not deployments. A new vertical is a `Vertical` row, a
theme, a category tree and a DNS record — not a fork.

## Backend

Clean Architecture, matching the pattern already running in the xcii
business-directory API (.NET 10, `Domain` / `Application` / `Infrastructure` /
`Api` / `Shared`, EF Core, central package management, test pyramid).

| Layer | Contents |
| --- | --- |
| `Domain` | Catalogue, Colour, Pricing, Ordering, Fulfilment aggregates. No EF, no HTTP. |
| `Application` | Use cases, validators, the price resolver, the coverage calculator |
| `Infrastructure` | EF Core + PostgreSQL, payment and courier adapters, search indexing |
| `Api` | Minimal APIs, auth, rate limiting, storefront and admin surfaces |

### Bounded contexts

Modules inside one deployable, with enforced boundaries. A modular monolith
until traffic justifies otherwise — splitting later is cheaper than distributed
transactions now.

| Context | Owns |
| --- | --- |
| **Catalogue** | Products, variants, categories, brands, media |
| **Colour** | Colour systems, colours, tint bases, availability, recipes |
| **Pricing** | Price resolution, tiers, promotions |
| **Ordering** | Cart, checkout, orders, returnability |
| **Fulfilment** | Shipments, rates, hazard rules, tracking |
| **Sellers** | Onboarding, offers, commission, payouts (Phase 3) |
| **Identity** | Customers, trade accounts, permissions |

Colour is its own context rather than a corner of Catalogue. It has its own
lifecycle, its own data sources, and in Phase 3 each manufacturer brings a
different colour system.

## Storefront

**Next.js**, server-rendered. The reasoning is SEO: organic search is most of
South African e-commerce discovery, and a client-rendered storefront concedes
that ground to Leroy Merlin and Takealot.

| Concern | Approach |
| --- | --- |
| Product pages | ISR — regenerate on catalogue change, serve static otherwise |
| Colour browser | Static payload, client-side filtering; 180 colours is small |
| Price display | Server-resolved; never compute price in the browser |
| Multi-vertical | One app, vertical resolved from hostname, theme from tokens |
| Feeds | Google Shopping / Merchant Centre feed generated per vertical |

## Cross-cutting

| Concern | Decision |
| --- | --- |
| Money | `decimal`, never float. Store ex-VAT, display inc-VAT, round once at the end. |
| VAT | 15%, display-inclusive per SA convention. Rate is data, not a constant. |
| Currency | ZAR only at launch. Stevensons' regional footprint makes this a Phase 4+ question. |
| Idempotency | Keys on checkout and payment callbacks. Gateways retry. |
| Audit | Append-only log on price, stock and order state changes |
| POPIA | Consent capture, data subject export and erasure endpoints from Phase 1 |
| Observability | Structured logs, traces on checkout and payment paths |

## Environments

`local` -> `staging` -> `production`. Staging runs against payment gateway
sandboxes and a courier test account. No production data in lower environments,
POPIA makes that a compliance issue rather than a preference.

## What is built

| Piece | Where | State |
| --- | --- | --- |
| Catalogue, Colour, Sellers, Pricing, Coverage, Ordering entities | `backend/src/Domain` | Done |
| Price resolver | `Domain/Pricing/PriceResolver.cs` | Done, 39 domain tests |
| Coverage calculator | `Domain/Coverage/CoverageCalculator.cs` | Done |
| Returnability policy | `Domain/Ordering/ReturnabilityPolicy.cs` | Done |
| Product pricing service | `Application/Paints` | Done, 10 tests |
| EF Core model, migration | `Infrastructure/Persistence` | Initial migration generated |
| Colour browse, pricing, coverage endpoints | `Api/Endpoints` | Done |
| Stevensons seed | `Api/Seeding` | 180 colours, one product |

Not yet built: cart and checkout persistence, payments, shipping rates, search,
admin, seller portal.
