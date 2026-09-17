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

## Implementation

One Next.js application. The vertical is resolved from the request hostname;
theme tokens, navigation and category tree follow from it. Adding Just Tools is
a row and a DNS record.
