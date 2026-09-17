# Just Things — commerce core

.NET 10, Clean Architecture. See `../docs/architecture.md` for the shape and
`../docs/decisions/` for why.

## Layout

| Project | Contains |
| --- | --- |
| `src/Domain` | Catalogue, Colour, Sellers, Pricing, Coverage, Ordering. No EF, no HTTP. |
| `src/Application` | Use cases composing domain rules |
| `src/Infrastructure` | EF Core + PostgreSQL, snake_case naming |
| `src/Api` | Minimal APIs, OpenAPI via Scalar |
| `src/Shared` | Cross-cutting helpers, e.g. ZAR formatting |

## The two pieces that matter

**`Domain/Pricing/PriceResolver.cs`** — price is resolved at runtime, never read
off a row, because the tint base is only known once a colour is chosen:

```
offer price + sheen uplift + (base uplift/L × pack litres) + colourant surcharge
```

then adjusted for the customer tier, VAT applied last, **rounded exactly once**.
`TotalIncVat` is authoritative; `VatAmount` is derived from it so an invoice line
plus its VAT always equals what the customer pays.

**`Domain/Coverage/CoverageCalculator.cs`** — turns square metres into the
cheapest combination of pack sizes, breaking cost ties towards the smaller
surplus so nobody is sold a spare 20L tin.

Both are pure and have no I/O, so both are covered by unit tests.

## Running

Postgres is the only dependency:

```bash
docker run --name justthings-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:17
```

Then:

```bash
dotnet run --project src/Api
```

In Development, `Seed:Enabled` is true: the app migrates the database and seeds
the Just Paints vertical, Stevensons, five tint bases, 180 Real Colours and one
product with nine variants. OpenAPI is at `/scalar/v1`.

## Tests

```bash
dotnet test
```

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
```

## Endpoints

| Route | Purpose |
| --- | --- |
| `GET /health` | Liveness plus a database check |
| `GET /api/colours?system=&q=&minLrv=` | Browse a colour system |
| `GET /api/products/{slug}/pricing?colour=RC53` | Resolve prices for a chosen colour |
| `POST /api/products/{slug}/coverage` | Turn an area into the cheapest basket |

A colour a product cannot be tinted to returns 404 rather than silently pricing
as factory white — there is genuinely no price to show.

## Seed data caveat

Colour names, codes and hex are Stevensons' own published Real Colours. LRV is
derived from hex. **Hue family and tint-base assignment are derived heuristics**
standing in until Stevensons supplies the real base mapping — that mapping is the
open Phase 0 ask, and without it no tinted line can be priced correctly.

**Product prices, pack sizes and weights are invented placeholders.** Do not
quote them.
