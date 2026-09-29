# ADR-0004: `Offer` is a first-class entity from day one

**Status:** Accepted
**Date:** 2026-09-17

## Context

Phase 1 has exactly one seller, Stevensons. The simplest model puts price and
stock directly on `ProductVariant`. True multi-seller support is a Phase 3
concern.

## Decision

Introduce `Offer` (seller x variant -> price, stock, lead time, fulfilment mode)
**immediately**, even with one seller.

## Consequences

Retrofitting `Offer` later means simultaneously rewriting pricing, cart,
checkout, order lines, shipment splitting, stock and reporting - while live and
taking money. It is the single most expensive thing to defer.

Carrying it early costs one extra join and a little indirection in Phase 1.

This is also what allows two sellers to list the same third-party product (a
Bosch drill in Phase 4) with a buy-box decision between them.

## Not included

Commission rules, settlement lines and payouts are modelled but not automated
until Phase 3. One seller reconciles in a spreadsheet.
