# Just Things

A South African marketplace for tools, hardware and home improvement, launching
through one specialist vertical at a time.

The first vertical is **Just Paints** (`paints.justthings.co.za`), anchored by
paint manufacturer [Stevensons](https://stevensons.co.za/) on a dropship
arrangement.

## Why one vertical first

A tin of paint is not a SKU, it is a configuration: `colour x base x sheen x
pack size`. General marketplaces model that badly, which is exactly the gap we
occupy. Build the platform multi-vertical and multi-seller; launch one vertical
and one seller.

## Repository layout

| Path | Contents |
| --- | --- |
| `brand/` | Corporate identity: logo, palette, approved mockups |
| `docs/` | Architecture, data model and decision records |
| `docs/decisions/` | ADRs — the reasoning behind irreversible choices |
| `prototype/` | Clickable Just Paints storefront prototype (static, no build step) |
| `backend/` | Commerce core: .NET 10, Clean Architecture, PostgreSQL |

## Running the prototype

No build step. Serve the folder:

```bash
npx serve prototype
```

## Strategy

Positioning, commercials, roadmap and risks live in the strategy document, not
in this repo.

## Status

Phase 0. The platform decision is taken: custom .NET 10 commerce core
(`docs/decisions/0001-commerce-platform.md`). Domain, pricing, coverage,
persistence and read endpoints are built; cart, checkout and payments are not.
