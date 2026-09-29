# Just Things - working notes

South African marketplace for tools, hardware and home improvement. Launching
through one vertical: **Just Paints** at `paints.justthings.co.za`, anchored by
Stevensons on a dropship arrangement (terms unconfirmed).

## Read first

- `docs/data-model.md` - the catalogue, colour and pricing model
- `docs/decisions/` - ADRs; 0002 is the one everything else depends on

## The rule that governs the codebase

**Colour is a line-item configuration, not a variant axis.** A `ProductVariant`
carries only sheen and pack size. The chosen colour rides on the cart/order line
with the tint base it resolves to.

Consequences that bite if forgotten:

- Price is **resolved at runtime**, never read off a row:
  `variant price + sheen uplift + (base uplift/L x litres)`, VAT applied last,
  rounded once at the end.
- The resolved price and the full colour record are **snapshotted onto the cart
  line**. Colours get renamed and withdrawn; orders must still print correctly.
- Returnability is computed **once at order placement** and stored. Tinted lines
  are excluded from the ECTA cooling-off right; untinted ones are not.

## Conventions

- Money is `decimal`, never float. Store ex-VAT, display inc-VAT (15%).
- ZAR only. Format as `R 1 234,56` - space thousands, comma decimal.
- South African English: colour, litre, organisation.
- `Offer` (seller x variant) is first-class even with one seller. Do not collapse
  it into `ProductVariant` - see ADR-0004.

## Brand

Palette is fixed: Prussian Blue `#23255A`, Sunset Gold `#EB9822`, Azure Blue
`#3986C7`. Tokens live in `brand/tokens.css`; never hardcode a hex in a
component. The approved homepage mockup is `brand/mockups/homepage-desktop.png`.

**Each vertical is its own brand.** The storefront is **Just Paints**, with its
own wordmark. It is never "Just Things Paints", and the Just Things wordmark
never appears with a category label attached. Just Things is the parent
marketplace and shows up only as a way back up (utility bar), an "is part of"
note in the footer, and the seller-facing brand. See ADR-0003.

The storefront is **light-only** (`data-theme="light"`). Paint colour judgement
needs a white surround, so dark mode is deliberately not offered on customer-
facing pages. Dark tokens exist for admin surfaces.

## Prototype

`prototype/` is a static clickable storefront - no build step, no framework.
Pages: `index.html`, `products.html` (listing, filters, colour preview),
`product.html` (configurator), `colours.html`.

Run it with the no-cache dev server - the stock `http.server` lets browsers
cache JS and CSS, so edits silently do not appear:

```bash
python tools/dev-server.py 4173 prototype
```

Products have no photography. Every tin is drawn by `JT.tinSvg(hex, opts)` in
`js/app.js`, which doubles as the colour preview: the lid and label stripe take
whatever colour is selected.

Colour data in `prototype/data/colours.js` is real: 180 names, RC codes and hex
captured from the rendered swatches on stevensons.co.za. `lrv` is derived from
hex; `family` and `tintBase` are **derived heuristics for the prototype only** -
real base assignment must come from Stevensons' tinting system.

`prototype/css/tokens.css` is a **copy** of `brand/tokens.css`, kept so the
prototype folder stays self-contained and zippable for a partner. `brand/` is the
source of truth - if you change tokens, copy the file across.

Everything in `prototype/data/catalogue.js` - products, prices, pack sizes,
weights - is invented and clearly flagged in-page. Do not quote those prices to
anyone.

## Backend

`backend/` - .NET 10, Clean Architecture, PostgreSQL with snake_case naming.
See `backend/README.md` to run it.

The two pieces that carry the product are pure and must stay that way:

- `Domain/Pricing/PriceResolver.cs`
- `Domain/Coverage/CoverageCalculator.cs`

No I/O in either. Both are fully unit-tested, and a change to the pricing rule
that does not also change `PriceResolverTests` is almost certainly a bug.

The database provider lives only in `Infrastructure/DependencyInjection.cs` and
`Persistence/DesignTimeDbContextFactory.cs`. Keep it that way.

## Status

Phase 0. Platform decision taken (ADR-0001): custom .NET 10. Domain, pricing,
coverage, persistence and read endpoints are built and green. Not yet built:
cart and checkout persistence, payments, shipping rates, search, admin.
