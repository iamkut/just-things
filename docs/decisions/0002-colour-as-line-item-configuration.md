# ADR-0002: Colour is a line-item configuration, not a variant axis

**Status:** Accepted
**Date:** 2026-09-17

## Context

A tin of paint is identified by `colour x base x sheen x pack size`. Treating
all four as variant axes produces roughly 2,160 variants for a single topcoat
range and about 10,800 across the five Stevensons ranges - against a few dozen
physically stocked items.

Every commerce platform stores variants as rows. Ten thousand mostly-fictional
rows per manufacturer is unworkable in all of them.

## Decision

`ProductVariant` carries **only the physically stocked axes**: sheen and pack
size. Roughly 12 variants per range.

**Colour is captured on the cart and order line** as a `LineConfiguration`,
alongside the tint base it resolves to.

A `ColourAvailability` join - `(productId, colourId) -> tintBaseId` - answers
both "can this be tinted to this colour" and "which base does it use".

## Consequences

- The catalogue stays proportional to real stock
- Price can no longer be read from a row; it must be **resolved at runtime**
  from `variant price + base uplift + colourant`, then snapshotted on the line
- Stock is tracked against bases, not against colour combinations, which
  matches how a paint factory actually works
- Search and feeds index products and colours separately rather than a
  combinatorial explosion of SKUs
- Any platform we choose must tolerate custom line-item pricing. This is what
  eliminates Shopify (ADR-0001).

## Notes

`hex` and `lrv` are nullable. Stevensons publishes colour names and RC codes but
not hex or light-reflectance values. The catalogue must render and sell before
that data is complete, degrading to swatch images and a hidden LRV filter.
