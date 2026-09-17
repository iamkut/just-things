# ADR-0003: Vertical storefronts as subdomains

**Status:** Accepted
**Date:** 2026-09-17

## Context

Just Things launches as a portfolio of specialist verticals — Just Paints first,
then others. The options were a path (`justthings.co.za/paints`), a subdomain
(`paints.justthings.co.za`), a separate domain, or a plain category.

## Decision

**Subdomains.** Just Paints lives at `paints.justthings.co.za`.

## Consequences

**Gaining:**
- Each vertical can be branded and marketed as a specialist destination, which
  is what makes a manufacturer want to anchor it
- Shared session, shared cart, shared account, shared infrastructure
- A vertical is data — a `Vertical` row, a theme, a category tree and a DNS
  record — not a separate deployment

**Accepting:**
- SEO authority is only partly shared with the apex domain; each vertical builds
  some of its own
- Cookie and session configuration must be set at the registrable domain so auth
  works across subdomains
- Analytics needs cross-subdomain tracking configured from day one, not
  retrofitted

## Branding

Each vertical is **its own brand**, not a badged version of the parent. The
storefront is **Just Paints** — its own wordmark, its own voice. It is never
"Just Things Paints", and the parent wordmark never appears with a category
label bolted onto it.

Just Things is the overarching marketplace and appears as:

- a way back up, in the utility bar above the masthead
- an "is part of Just Things" note in the footer
- the seller-facing brand ("Sell on Just Things")

The reason is commercial, not cosmetic. A manufacturer anchors a vertical
because it reads as a specialist destination. A category page wearing a parent
marketplace's logo reads as a shelf, and is worth much less to them.

The shared platform underneath is unaffected — one codebase, one cart, one
account. Shared infrastructure, separate identities.

## Implementation

One Next.js application. The vertical is resolved from the request hostname;
theme tokens, navigation, category tree **and brand lockup** follow from it.
Adding Just Tools is a row, a lockup and a DNS record.
