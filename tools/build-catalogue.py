"""
Generate prototype/data/catalogue.js from stevensons-catalogue.json.

The prototype expects a specific product shape; the scraped Stevensons data is
richer and messier. This script does the transformation:

  - Normalises "appearance" ("Smooth Silk Finish" → "Silk") to one of the
    prototype's canonical sheens.
  - Classifies each product into a storefront category by range + keywords.
  - Derives packs from (size × white-base variation price), converting
    inc-VAT to ex-VAT so the prototype's VAT-on-top model stays coherent.
  - Keeps the original pricing config (tint bases, VAT, sheen uplift, sample
    pot) copied over verbatim.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "prototype" / "data" / "stevensons-catalogue.json"
DST = ROOT / "prototype" / "data" / "catalogue.js"

VAT = 0.15  # must match JT_VAT_RATE

# -----------------------------------------------------------------------------
# Classification
# -----------------------------------------------------------------------------

CATEGORIES = [
    "Interior walls",
    "Exterior walls",
    "Roof",
    "Wood care",
    "Enamel & metal",
    "Primers & undercoats",
    "Waterproofing",
    "Preparation & cleaning",
    "Specialty",
]


def classify(range_name: str, product_name: str) -> str:
    r = (range_name or "").lower()
    n = (product_name or "").lower()

    if "durawood" in r:
        return "Wood care"
    if "metalrite" in r:
        return "Enamel & metal"
    if "sealtite" in r or "waterproofing" in r:
        return "Waterproofing"
    if "plasterskim" in r:
        return "Preparation & cleaning"
    if "fast range cleaning" in r or "fast range preparation" in r:
        return "Preparation & cleaning"
    if "fast range metal" in r or "fast range automotive" in r:
        return "Enamel & metal"
    if "rapid" in r:
        return "Specialty"

    # By product-name keywords for the big ranges (Architect, Crest, Dura,
    # Industrial, Professional, Tradesman).
    if any(k in n for k in ("roof", "roofmaster", "trusskote")):
        return "Roof"
    if any(k in n for k in ("enamel", "linenglo", "linen glo", "high heat")):
        return "Enamel & metal"
    if any(k in n for k in ("primer", "undercoat", "first coater", "bonding liquid", "pots & concrete", "etchkote")):
        return "Primers & undercoats"
    if any(k in n for k in ("floorkote",)):
        return "Enamel & metal"
    if any(k in n for k in ("crack bridge", "silver shield", "bituminous", "roadline")):
        return "Specialty"
    if "wallkote" in n or "topgard" in n or "texguard" in n or "exterior only" in n or ("exterior" in n and "interior" not in n):
        return "Exterior walls"
    if "ceiling" in n or "kitchen & bathroom" in n or "arctic white" in n:
        return "Interior walls"
    # Everything else that reads as a wall paint
    if any(k in n for k in ("allklad", "linen matt", "linen silk", "midsheen", "pva", "wall", "masterkote", "sheen coat", "coverall", "polmax")):
        return "Interior walls"

    return "Specialty"


# -----------------------------------------------------------------------------
# Sheen normalisation
# -----------------------------------------------------------------------------

SHEEN_PATTERNS: list[tuple[re.Pattern, str]] = [
    (re.compile(r"\beggshell\b", re.I), "Eggshell"),
    (re.compile(r"\b(semi[-\s]?gloss|high\s?gloss|gloss)\b", re.I), "Gloss"),
    (re.compile(r"\bsilk\b", re.I), "Silk"),
    (re.compile(r"\bsatin\b", re.I), "Satin"),
    (re.compile(r"\b(semi[-\s]?matt|matt)\b", re.I), "Matt"),
    (re.compile(r"\b(low\s?sheen|medium\s?sheen|smooth\s?sheen|sheen)\b", re.I), "Silk"),
    (re.compile(r"\btextured\b", re.I), "Textured"),
]

NON_PAINT_WORDS = ("liquid", "paste", "powder", "clear", "gel", "solid")


def derive_sheen(appearance: str | None, product_name: str) -> str | None:
    """Pick a canonical sheen from the brochure "appearance" or product name."""
    candidates = [appearance or "", product_name or ""]
    for text in candidates:
        if not text:
            continue
        # Skip rows that are clearly describing a liquid/powder product.
        low = text.lower()
        if any(w in low for w in NON_PAINT_WORDS) and "finish" not in low:
            continue
        for pattern, sheen in SHEEN_PATTERNS:
            if pattern.search(text):
                return sheen
    return None


# -----------------------------------------------------------------------------
# Coverage defaults by category (used when brochure has no coverage)
# -----------------------------------------------------------------------------

DEFAULT_COVERAGE = {
    "Interior walls": 8.0,
    "Exterior walls": 7.0,
    "Roof": 6.0,
    "Wood care": 12.0,
    "Enamel & metal": 11.0,
    "Primers & undercoats": 8.0,
    "Waterproofing": 4.0,
    "Preparation & cleaning": None,  # not applicable
    "Specialty": None,
}


# -----------------------------------------------------------------------------
# Name cleanup
# -----------------------------------------------------------------------------

NAME_SUFFIX_RE = re.compile(
    r"\s*[–—‒-]\s*(interior\s*&\s*exterior|interior\s*only|exterior\s*only|interior|exterior)\s*$",
    re.IGNORECASE,
)


def pretty_name(raw: str) -> str:
    """Convert "ARCHITECT ALLKLAD – Interior & exterior" into "Architect AllKlad"."""
    s = NAME_SUFFIX_RE.sub("", raw).strip()
    # Title-case but preserve a few known mixed-case bits ("AllKlad", "LinenGlo", "PVA").
    tokens = []
    for word in s.split():
        if len(word) <= 3 and word.isupper():
            tokens.append(word)              # PVA, QD, etc.
        elif word.upper() == word:
            tokens.append(word.title())
        else:
            tokens.append(word)
    out = " ".join(tokens)
    out = out.replace("Allklad", "AllKlad").replace("Linenglo", "LinenGlo").replace("Wallkote", "WallKote")
    return out


# -----------------------------------------------------------------------------
# Pack extraction
# -----------------------------------------------------------------------------

SIZE_RE = re.compile(r"(\d+(?:\.\d+)?)\s*l", re.IGNORECASE)


def parse_litres(size: str) -> float | None:
    m = SIZE_RE.search(size)
    return float(m.group(1)) if m else None


def build_packs(product: dict) -> list[dict]:
    """One pack per size, priced from the cheapest (white base) variation at that size."""
    packs: list[dict] = []
    by_size: dict[str, list[float]] = {}
    for v in product.get("variations", []):
        size = v.get("size") or ""
        price = v.get("price") or 0
        if size and price:
            by_size.setdefault(size, []).append(price)

    for size in product.get("sizes", []):
        litres = parse_litres(size)
        prices = by_size.get(size, [])
        if not litres or not prices:
            continue
        inc_vat = min(prices)             # white base ≈ lowest-priced variation
        ex_vat = round(inc_vat / (1 + VAT), 2)
        weight_kg = round(litres * 1.2, 2)
        packs.append({"litres": litres, "priceExVat": ex_vat, "weightKg": weight_kg})

    packs.sort(key=lambda p: p["litres"])
    return packs


# -----------------------------------------------------------------------------
# Hazard
# -----------------------------------------------------------------------------

def derive_hazard(product: dict) -> str | None:
    fp = (product.get("flash_point") or "").lower()
    if "non" in fp and "flammable" in fp:
        return None
    if "flammable" in fp:
        return "Flammable"
    # Look at product name for solvent/thinner/bituminous markers
    name = (product.get("name") or "").lower()
    if any(w in name for w in ("solvent based", "bituminous", "qd enamel", "stripper", "thinner")):
        return "Flammable"
    return None


# -----------------------------------------------------------------------------
# Main transform
# -----------------------------------------------------------------------------

def transform(src: dict) -> list[dict]:
    out: list[dict] = []
    for p in src["products"]:
        packs = build_packs(p)
        if not packs:
            continue  # no price = can't quote; skip

        category = classify(p.get("range") or "", p.get("name") or "")
        sheen = derive_sheen(p.get("appearance"), p.get("name") or "")
        coverage = p.get("coverage_per_litre") or DEFAULT_COVERAGE.get(category) or 8.0

        name = pretty_name(p.get("name") or "")
        range_name = (p.get("range") or "").title().replace(" Range", "")
        if range_name.endswith(" Range"):
            range_name = range_name[:-6]

        out.append({
            "id": p["slug"],
            "slug": p["slug"],
            "brand": p.get("brand") or "Stevensons",
            "range": range_name or (p.get("brand") or "Stevensons"),
            "name": name,
            "blurb": p.get("short_description") or "",
            "category": category,
            "coveragePerLitre": coverage,
            "coatsRecommended": p.get("coats_recommended") or 2,
            "sheens": [sheen] if sheen else [],
            "packs": packs,
            "image": p.get("hero_image_local"),
            "hazard": derive_hazard(p),
            "colourCount": len(p.get("colours") or []),
            "features": (p.get("features") or [])[:6],
            "tdsUrl": p.get("tds_url"),
            "sdsUrl": p.get("sds_url"),
        })
    return out


HEADER = '''\
/* AUTOGENERATED by tools/build-catalogue.py from stevensons-catalogue.json.
   Do not hand-edit. Re-run the script after refetching the scrape. */

window.JT_TINT_BASES = {
  White:  { label: "White base",  upliftPerLitre: 0 },
  Pastel: { label: "Pastel base", upliftPerLitre: 6 },
  Medium: { label: "Medium base", upliftPerLitre: 14 },
  Deep:   { label: "Deep base",   upliftPerLitre: 26 },
  Accent: { label: "Accent base", upliftPerLitre: 38 }
};

window.JT_VAT_RATE = 0.15;

window.JT_SAMPLE_POT = { litres: 0.25, priceExVat: 69.00, weightKg: 0.4 };

/* Sheen uplift stays at zero across the board: Stevensons sells each product
   at a single baked-in finish, so the finish is reflected in the shop price,
   not applied as a surcharge. The map is kept so the resolver still runs. */
window.JT_SHEEN_UPLIFT = { Matt: 0, Silk: 0, Satin: 0, Eggshell: 0, Gloss: 0, Textured: 0 };

'''


def main() -> int:
    src = json.loads(SRC.read_text(encoding="utf-8"))
    products = transform(src)

    body = HEADER + "window.JT_PRODUCTS = " + json.dumps(products, indent=2, ensure_ascii=False) + ";\n"
    DST.write_text(body, encoding="utf-8")

    # Report
    counts: dict[str, int] = {}
    sheen_counts: dict[str | None, int] = {}
    for p in products:
        counts[p["category"]] = counts.get(p["category"], 0) + 1
        s = p["sheens"][0] if p["sheens"] else None
        sheen_counts[s] = sheen_counts.get(s, 0) + 1

    print(f"wrote {DST.relative_to(ROOT)} ({len(products)} products)")
    print("by category:")
    for c in CATEGORIES:
        print(f"  {counts.get(c, 0):3d}  {c}")
    print("by sheen:")
    for s, n in sorted(sheen_counts.items(), key=lambda x: -x[1]):
        print(f"  {n:3d}  {s!r}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
