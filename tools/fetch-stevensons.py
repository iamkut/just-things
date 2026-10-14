"""
Fetch product information and images from stevensons.co.za.

Authorised use: Stevensons is a Just Things client and has granted permission
to use their product images and information on the Just Paints storefront.

Writes:
  prototype/data/stevensons-catalogue.json        structured catalogue
  prototype/assets/stevensons/products/<sku>.png  one hero image per product
  prototype/assets/stevensons/colours/<slug>.png  per-colour tin renders

Behaviour:
  - Rate-limited to one request per second.
  - Resumable: skips images already on disk.
  - Captures pricing into JSON but downstream code is expected to treat
    retail pricing as pending-confirmation (see CLAUDE.md).

Usage:
    python tools/fetch-stevensons.py             # full crawl
    python tools/fetch-stevensons.py --limit 3   # smoke-test on 3 products
    python tools/fetch-stevensons.py --no-images # metadata only
"""
from __future__ import annotations

import argparse
import difflib
import html
import json
import re
import sys
import time
from dataclasses import asdict, dataclass, field
from pathlib import Path
from urllib.parse import urlparse

import requests
from bs4 import BeautifulSoup

ROOT = Path(__file__).resolve().parent.parent
DATA_FILE = ROOT / "prototype" / "data" / "stevensons-catalogue.json"
IMG_DIR = ROOT / "prototype" / "assets" / "stevensons"
PRODUCT_IMG_DIR = IMG_DIR / "products"
COLOUR_IMG_DIR = IMG_DIR / "colours"

BASE = "https://stevensons.co.za"
LISTING = BASE + "/products/?product-page={page}"
UA = "JustPaints-Catalogue-Fetcher/0.1 (+kutlom@aissa.co.za)"

session = requests.Session()
session.headers["User-Agent"] = UA

_last_request = 0.0


def get(url: str) -> requests.Response:
    """GET with a 1 req/sec floor. Forces utf-8 so m² decodes correctly."""
    global _last_request
    gap = time.time() - _last_request
    if gap < 1.0:
        time.sleep(1.0 - gap)
    r = session.get(url, timeout=30)
    _last_request = time.time()
    r.raise_for_status()
    # requests guesses from headers; stevensons.co.za serves utf-8 but the
    # declared charset can trick chardet. Force utf-8 for text parsing.
    if r.headers.get("content-type", "").lower().startswith(("text/", "application/json", "application/xml")):
        r.encoding = "utf-8"
    return r


@dataclass
class Variation:
    size: str
    colour_slug: str
    colour_name: str
    colour_hex: str | None
    price: float
    image_url: str | None


@dataclass
class Product:
    url: str
    slug: str
    sku: str | None
    name: str
    brand: str | None
    range: str | None
    short_description: str
    sizes: list[str] = field(default_factory=list)
    colours: list[dict] = field(default_factory=list)
    price_range: list[float] | None = None
    hero_image_url: str | None = None
    hero_image_local: str | None = None
    tds_url: str | None = None
    sds_url: str | None = None
    variations: list[Variation] = field(default_factory=list)
    # Enriched from the matching /product-info/ brochure page.
    brochure_url: str | None = None
    appearance: str | None = None             # e.g. "Smooth Silk Finish"
    product_type: str | None = None           # e.g. "Acrylic"
    guarantee: str | None = None              # e.g. "15 Years"
    project_type: str | None = None           # e.g. "Exterior, Interior"
    flash_point: str | None = None            # e.g. "Non-flammable" / "<23°C"
    theoretical_coverage: str | None = None   # e.g. "9-12m²/L per coat"
    coverage_per_litre: float | None = None   # numeric midpoint of theoretical_coverage
    coats_recommended: int | None = None      # parsed from Application: "two coats" -> 2
    tint_bases: list[str] = field(default_factory=list)
    features: list[str] = field(default_factory=list)
    long_description: str | None = None


def discover_product_urls() -> list[str]:
    urls: list[str] = []
    seen: set[str] = set()
    page = 1
    while True:
        r = get(LISTING.format(page=page))
        soup = BeautifulSoup(r.content, "html.parser")
        found_this_page = 0
        for a in soup.select("a.woocommerce-LoopProduct-link, a.woocommerce-loop-product__link, li.product a[href*='/product/']"):
            href = a.get("href", "")
            if "/product/" not in href:
                continue
            # Strip query/fragment so colour-variant permalinks collapse onto the base product URL.
            parsed = urlparse(href)
            canonical = f"{parsed.scheme}://{parsed.netloc}{parsed.path}"
            if canonical not in seen:
                seen.add(canonical)
                urls.append(canonical)
                found_this_page += 1
        if found_this_page == 0:
            break
        page += 1
        if page > 20:  # hard safety
            break
    return urls


_HEX_RE = re.compile(r"-([0-9a-f]{6})$", re.IGNORECASE)


def _hex_from_slug(slug: str) -> str | None:
    m = _HEX_RE.search(slug)
    return "#" + m.group(1).lower() if m else None


def parse_product(url: str) -> Product:
    r = get(url)
    soup = BeautifulSoup(r.content, "html.parser")

    name = (soup.select_one(".product_title") or soup.find("h1")).get_text(strip=True)
    short = soup.select_one(".woocommerce-product-details__short-description")
    short_text = short.get_text(" ", strip=True) if short else ""

    sku_el = soup.select_one(".sku")
    sku = sku_el.get_text(strip=True) if sku_el else None

    brand = range_ = None
    cats = soup.select(".posted_in a") or soup.select(".product_meta a[rel~=tag]")
    cat_names = [c.get_text(strip=True) for c in cats]
    for name_cat in cat_names:
        if "RANGE" in name_cat.upper():
            range_ = name_cat
        else:
            brand = name_cat
    if not brand and cat_names:
        brand = cat_names[-1]

    hero = None
    gallery_img = soup.select_one(".woocommerce-product-gallery img")
    if gallery_img:
        hero = gallery_img.get("data-large_image") or gallery_img.get("data-src") or gallery_img.get("src")

    tds = sds = None
    for a in soup.find_all("a"):
        text = a.get_text(strip=True).upper()
        if text == "TDS":
            tds = a.get("href")
        elif text == "SDS":
            sds = a.get("href")

    variations: list[Variation] = []
    sizes: list[str] = []
    colours_map: dict[str, dict] = {}

    form = soup.select_one("form.variations_form")
    if form and form.get("data-product_variations"):
        try:
            raw = json.loads(form["data-product_variations"])
        except Exception:
            raw = []
        # WooCommerce sets data-product_variations="false" when the product has
        # too many variations to inline (AJAX fallback). Treat as "no inline data".
        if not isinstance(raw, list):
            raw = []
        for v in raw:
            attrs = v.get("attributes", {})
            size_slug = attrs.get("attribute_pa_size") or ""
            colour_slug = attrs.get("attribute_pa_colour") or ""
            size = size_slug.upper() if size_slug else ""
            colour_hex = _hex_from_slug(colour_slug)
            colour_name = colour_slug.rsplit("-", 1)[0].replace("-", " ").strip().title() if colour_slug else ""
            price = float(v.get("display_price") or 0)
            img_url = (v.get("image") or {}).get("url") if v.get("image") else None
            variations.append(Variation(size, colour_slug, colour_name, colour_hex, price, img_url))
            if size and size not in sizes:
                sizes.append(size)
            if colour_slug and colour_slug not in colours_map:
                colours_map[colour_slug] = {"slug": colour_slug, "name": colour_name, "hex": colour_hex}

    prices = [v.price for v in variations if v.price]
    price_range = [min(prices), max(prices)] if prices else None

    slug = urlparse(url).path.rstrip("/").rsplit("/", 1)[-1]

    return Product(
        url=url,
        slug=slug,
        sku=sku,
        name=name,
        brand=brand,
        range=range_,
        short_description=short_text,
        sizes=sorted(sizes, key=lambda s: int(re.search(r"\d+", s).group()) if re.search(r"\d+", s) else 0),
        colours=list(colours_map.values()),
        price_range=price_range,
        hero_image_url=hero,
        tds_url=tds,
        sds_url=sds,
        variations=variations,
    )


# -------------------------------------------------------------------------
# Brochure enrichment (from /product-info/ pages).
#
# The shop pages carry commerce data (variations, prices, images). The sibling
# brochure pages carry the product spec. Both exist for most products but their
# slugs don't share a canonical form, so we match them by normalised title.
# -------------------------------------------------------------------------

_DASH_RE = re.compile(r"[–—‒-]")  # en/em dashes
_PAREN_RE = re.compile(r"\s*\(.*?\)\s*")
_SUFFIX_RE = re.compile(
    r"\s*(?:interior\s*&\s*exterior|interior\s*only|exterior\s*only|interior|exterior)\s*$",
    re.IGNORECASE,
)
_PREFIX_RE = re.compile(r"^(?:stevensons|crest|durawood|metalrite|fast|sealtite|plasterskim|rapid|dura)\s+", re.IGNORECASE)
_WS_RE = re.compile(r"\s+")


_FILLER_WORDS = {"the", "and", "a", "of", "for", "with", "premium", "pure", "quality"}


def _title_tokens(raw: str) -> frozenset[str]:
    s = html.unescape(raw).lower()
    s = _DASH_RE.sub(" ", s)
    s = _PAREN_RE.sub(" ", s)
    s = _SUFFIX_RE.sub(" ", s)
    s = _PREFIX_RE.sub(" ", s)
    s = re.sub(r"[^a-z0-9]+", " ", s)
    tokens = {t for t in s.split() if t and t not in _FILLER_WORDS}
    return frozenset(tokens)


def discover_brochure_links() -> list[tuple[frozenset[str], str, str]]:
    """Return a list of (tokens, title, url) for every brochure in the WP REST API."""
    out: list[tuple[frozenset[str], str, str]] = []
    page = 1
    while True:
        r = get(f"{BASE}/wp-json/wp/v2/product-info?per_page=100&page={page}")
        batch = r.json()
        if not batch:
            break
        for item in batch:
            title = (item.get("title") or {}).get("rendered") or ""
            link = item.get("link") or ""
            if title and link:
                out.append((_title_tokens(title), html.unescape(title), link))
        total_pages = int(r.headers.get("X-WP-TotalPages", 1))
        if page >= total_pages:
            break
        page += 1
    return out


def _match_brochure(product_name: str, index: list[tuple[frozenset[str], str, str]]) -> str | None:
    """Pick the brochure whose token-set best matches the shop product's tokens.

    First pass: strict subset of shop tokens must appear in brochure tokens.
    Fallback: character-level similarity on concatenated tokens, to catch
    compound/split-word mismatches like "LinenGlo" vs "Linen Glo".
    """
    pt = _title_tokens(product_name)
    if not pt:
        return None
    best: tuple[float, str] | None = None
    for bt, _title, url in index:
        if not bt or not pt.issubset(bt):
            continue
        jaccard = len(pt & bt) / len(pt | bt)
        if best is None or jaccard > best[0]:
            best = (jaccard, url)
    if best:
        return best[1]

    # Fallback: compare concatenated-token strings. Tolerates "linenglo" ↔ "linen glo".
    pc = "".join(sorted(pt))
    best_ratio = 0.0
    best_url: str | None = None
    for bt, _title, url in index:
        if not bt:
            continue
        bc = "".join(sorted(bt))
        ratio = difflib.SequenceMatcher(None, pc, bc).ratio()
        if ratio > best_ratio:
            best_ratio, best_url = ratio, url
    return best_url if best_ratio >= 0.78 else None


# Labels where the value sits on the same line, no colon in the source.
_SPEC_LABELS_NO_COLON = [
    "Project Type",
    "Available sizes",
    "Guarantee",
    "Product Type",
]
# Labels that are followed by a colon in the source (e.g. "Application: Apply two coats").
# These must require the colon in the pattern, otherwise generic words like "Application"
# will also match section headings like "Application Details".
_SPEC_LABELS_COLON = [
    "Type",
    "Appearance",
    "Colours",
    "Tint Bases",
    "Solid Approx.",
    "Flash point",
    "Theoretical spreading rate",
    "Practical spreading rate",
    "Mixing",
    "Method",
    "Thinning",
    "Cleaning",
    "Drying time",
    "Recoating time",
    "Application",
]
_SPEC_LABELS = _SPEC_LABELS_NO_COLON + _SPEC_LABELS_COLON

# Section fence posts: not value fields, but we don't want greedy value matches to
# swallow them. Treat them as terminators when looking for the next-label boundary.
_SECTION_FENCES = [
    "Additional Information",
    "Features & Benefits",
    "Product Information",
    "Application Details",
]


def _extract_specs(text: str) -> dict[str, str]:
    """Pull known label:value pairs out of a flat text blob."""
    specs: dict[str, str] = {}
    terminators = _SPEC_LABELS + _SECTION_FENCES
    for label in _SPEC_LABELS:
        colon_part = r"[:\-]" if label in _SPEC_LABELS_COLON else r"[:\-]?"
        others = "|".join(re.escape(ol) for ol in terminators if ol != label)
        pattern = rf"\b{re.escape(label)}\s*{colon_part}\s*(.+?)(?=\s+(?:{others})\s*[:\-]?\s|$)"
        m = re.search(pattern, text, re.IGNORECASE | re.DOTALL)
        if m:
            specs[label] = _WS_RE.sub(" ", m.group(1)).strip(" .,")
    return specs


def _coverage_midpoint(coverage: str) -> float | None:
    if not coverage:
        return None
    nums = re.findall(r"\d+(?:[.,]\d+)?", coverage)
    if not nums:
        return None
    vals = [float(n.replace(",", ".")) for n in nums[:2]]
    return round(sum(vals) / len(vals), 1)


def _parse_coats(text: str) -> int | None:
    if not text:
        return None
    words = {"one": 1, "two": 2, "three": 3, "four": 4}
    low = text.lower()
    for word, n in words.items():
        if re.search(rf"\b{word}\s+coats?\b", low):
            return n
    m = re.search(r"(\d+)\s+coats?", low)
    return int(m.group(1)) if m else None


def enrich_from_brochure(product: Product, brochure_url: str) -> None:
    r = get(brochure_url)
    soup = BeautifulSoup(r.content, "html.parser", from_encoding="utf-8")

    article = soup.select_one(".entry-content, article, main")
    if not article:
        return
    text = article.get_text("\n", strip=True)

    specs = _extract_specs(text)

    def _fix_mojibake(s: str | None) -> str | None:
        if not s:
            return s
        # The brochure pages are double-encoded in places: utf-8 bytes read as
        # latin-1 and re-encoded, so ² (U+00B2) arrives as "Â²".
        return s.replace("Â²", "²").replace("Â°", "°").replace("Â·", "·")

    product.brochure_url = brochure_url
    product.appearance = _fix_mojibake(specs.get("Appearance"))
    product.product_type = _fix_mojibake(specs.get("Product Type"))
    product.guarantee = _fix_mojibake(specs.get("Guarantee"))
    product.project_type = _fix_mojibake(specs.get("Project Type"))
    product.flash_point = _fix_mojibake(specs.get("Flash point"))
    product.theoretical_coverage = _fix_mojibake(specs.get("Theoretical spreading rate"))
    product.coverage_per_litre = _coverage_midpoint(product.theoretical_coverage or "")
    product.coats_recommended = _parse_coats(specs.get("Application", ""))

    tint_bases_raw = specs.get("Tint Bases") or ""
    product.tint_bases = [t.strip() for t in re.split(r"[,/]", tint_bases_raw) if t.strip()]

    # Features & Benefits appears between the "Features & Benefits" heading and the next labelled block.
    m = re.search(
        r"Features\s*&\s*Benefits\s*(.*?)(?=\n\s*(?:Product Information|Application Details|Additional Information)\b)",
        text,
        re.IGNORECASE | re.DOTALL,
    )
    if m:
        product.features = [
            line.strip(" •·-–—")
            for line in m.group(1).splitlines()
            if line.strip() and len(line.strip()) < 220
        ]

    # Long description = short description plus any extra prose above the Additional Information block.
    pre = re.split(r"\n\s*Additional Information\b", text, maxsplit=1, flags=re.IGNORECASE)[0]
    product.long_description = pre.strip() or None


def download(url: str, dest: Path) -> bool:
    if dest.exists() and dest.stat().st_size > 0:
        return False
    dest.parent.mkdir(parents=True, exist_ok=True)
    r = get(url)
    dest.write_bytes(r.content)
    return True


def _ext_from_url(url: str) -> str:
    path = urlparse(url).path
    for ext in (".png", ".jpg", ".jpeg", ".webp"):
        if path.lower().endswith(ext):
            return ext
    return ".png"


def download_images(products: list[Product]) -> None:
    for p in products:
        if p.hero_image_url:
            ext = _ext_from_url(p.hero_image_url)
            dest = PRODUCT_IMG_DIR / f"{p.slug}{ext}"
            try:
                if download(p.hero_image_url, dest):
                    print(f"  downloaded {dest.name}")
            except Exception as e:
                print(f"  !! hero failed for {p.slug}: {e}", file=sys.stderr)
                continue
            p.hero_image_local = str(dest.relative_to(ROOT / "prototype")).replace("\\", "/")

        seen_colour_urls: set[str] = set()
        for v in p.variations:
            if not v.image_url or v.image_url in seen_colour_urls:
                continue
            seen_colour_urls.add(v.image_url)
            if v.image_url == p.hero_image_url:
                continue
            ext = _ext_from_url(v.image_url)
            dest = COLOUR_IMG_DIR / f"{p.slug}--{v.colour_slug}{ext}"
            try:
                if download(v.image_url, dest):
                    print(f"  downloaded {dest.name}")
            except Exception as e:
                print(f"  !! colour image failed for {p.slug}/{v.colour_slug}: {e}", file=sys.stderr)


def write_catalogue(products: list[Product]) -> None:
    payload = {
        "source": "stevensons.co.za",
        "fetched_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "note": "Pricing captured for reference; treat as pending-confirmation per CLAUDE.md.",
        "products": [
            {**asdict(p), "variations": [asdict(v) for v in p.variations]}
            for p in products
        ],
    }
    DATA_FILE.parent.mkdir(parents=True, exist_ok=True)
    DATA_FILE.write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"wrote {DATA_FILE.relative_to(ROOT)} ({len(products)} products)")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--limit", type=int, default=0, help="stop after N products (0 = all)")
    parser.add_argument("--no-images", action="store_true", help="skip image downloads")
    parser.add_argument("--no-brochures", action="store_true", help="skip brochure spec enrichment")
    parser.add_argument("--only", help="comma-separated product slug substrings to include")
    args = parser.parse_args()

    print("discovering product URLs...")
    urls = discover_product_urls()
    print(f"found {len(urls)} products")

    if args.only:
        needles = [s.strip().lower() for s in args.only.split(",") if s.strip()]
        urls = [u for u in urls if any(n in u.lower() for n in needles)]
        print(f"filtered to {len(urls)} by --only")

    if args.limit:
        urls = urls[: args.limit]
        print(f"limited to {len(urls)}")

    products: list[Product] = []
    for i, url in enumerate(urls, 1):
        print(f"[{i}/{len(urls)}] {url}")
        try:
            products.append(parse_product(url))
        except Exception as e:
            print(f"  !! parse failed: {e}", file=sys.stderr)

    if not args.no_brochures:
        print("discovering brochure URLs via WP REST API...")
        brochures = discover_brochure_links()
        print(f"found {len(brochures)} brochures")
        matched = unmatched = 0
        for p in products:
            url = _match_brochure(p.name, brochures)
            if not url:
                unmatched += 1
                print(f"  (no brochure) {p.name}")
                continue
            try:
                enrich_from_brochure(p, url)
                matched += 1
            except Exception as e:
                print(f"  !! brochure enrichment failed for {p.slug}: {e}", file=sys.stderr)
                unmatched += 1
        print(f"enriched {matched}/{len(products)} products ({unmatched} without a matching brochure)")

    if not args.no_images:
        print("downloading images...")
        download_images(products)

    write_catalogue(products)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
