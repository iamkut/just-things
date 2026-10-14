"""
Extract embedded product images from the client's Stevensons_Product_Catalog.xlsx
and write them under prototype/data/images/stevensons/<slug>/ keyed by the
Source URL cell on the same spreadsheet row.

Why this exists: the Oct 2026 client hand-off bundle has 100 cleaner product
shots embedded in the catalogue xlsx (one per row). The scraper's hero_image_url
points at stevensons.co.za-hosted assets; this gives us a local copy keyed to
the slug the scraper already uses.

Usage:
    python tools/extract-stevensons-xlsx-images.py <path-to-xlsx>

Writes:
    prototype/data/images/stevensons/<slug>/catalogue-hero.<ext>
    prototype/data/images/stevensons/_unmatched/image<N>.<ext>  (if no URL)
"""
from __future__ import annotations

import sys
import zipfile
from pathlib import Path
from urllib.parse import urlparse
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "prototype" / "data" / "images" / "stevensons"

NS = {
    "s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
    "xdr": "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
}


def _shared_strings(z: zipfile.ZipFile) -> list[str]:
    root = ET.fromstring(z.read("xl/sharedStrings.xml"))
    out: list[str] = []
    for si in root.findall("s:si", NS):
        parts = [t.text or "" for t in si.iter(f"{{{NS['s']}}}t")]
        out.append("".join(parts))
    return out


def _rows(z: zipfile.ZipFile, sheet_path: str, strings: list[str]) -> list[list[str]]:
    root = ET.fromstring(z.read(sheet_path))
    sd = root.find("s:sheetData", NS)
    out: list[list[str]] = []
    for r in sd.findall("s:row", NS):
        row: list[str] = []
        for c in r.findall("s:c", NS):
            t = c.attrib.get("t", "n")
            v = c.find("s:v", NS)
            if v is None:
                row.append("")
                continue
            row.append(strings[int(v.text)] if t == "s" else (v.text or ""))
        out.append(row)
    return out


def _slug_from_url(url: str) -> str | None:
    if not url.startswith("http"):
        return None
    return urlparse(url).path.rstrip("/").rsplit("/", 1)[-1] or None


def _sheet_rel_map(z: zipfile.ZipFile) -> dict[str, str]:
    """sheet<n>.xml -> drawing<m>.xml via the sheet's rels file."""
    out: dict[str, str] = {}
    for name in z.namelist():
        if not name.startswith("xl/worksheets/_rels/") or not name.endswith(".xml.rels"):
            continue
        sheet = name.split("/")[-1].replace(".rels", "")
        root = ET.fromstring(z.read(name))
        for rel in root.findall("pr:Relationship", NS):
            target = rel.attrib.get("Target", "")
            if "drawing" in target:
                drawing = target.rsplit("/", 1)[-1]
                out[f"xl/worksheets/{sheet}"] = f"xl/drawings/{drawing}"
    return out


def _drawing_rel_map(z: zipfile.ZipFile, drawing_path: str) -> dict[str, str]:
    """rId -> media/<name> for a single drawing's rels file."""
    rels_name = drawing_path.replace("drawings/", "drawings/_rels/") + ".rels"
    root = ET.fromstring(z.read(rels_name))
    out: dict[str, str] = {}
    for rel in root.findall("pr:Relationship", NS):
        target = rel.attrib.get("Target", "")
        # target like ../media/image12.png
        media = target.split("/")[-1]
        out[rel.attrib["Id"]] = f"xl/media/{media}"
    return out


def _row_to_image(z: zipfile.ZipFile, drawing_path: str) -> dict[int, str]:
    """0-indexed sheet row -> xl/media/<file> for every anchored image on this drawing."""
    rel_map = _drawing_rel_map(z, drawing_path)
    root = ET.fromstring(z.read(drawing_path))
    out: dict[int, str] = {}
    for anchor in root.iter(f"{{{NS['xdr']}}}twoCellAnchor"):
        frm = anchor.find("xdr:from", NS)
        row_el = frm.find("xdr:row", NS) if frm is not None else None
        if row_el is None or row_el.text is None:
            continue
        row = int(row_el.text)
        blip = anchor.find(".//{http://schemas.openxmlformats.org/drawingml/2006/main}blip")
        if blip is None:
            continue
        rid = blip.attrib.get(f"{{{NS['r']}}}embed")
        media = rel_map.get(rid or "")
        if media:
            out[row] = media
    return out


def _ext_of(media_path: str) -> str:
    return "." + media_path.rsplit(".", 1)[-1].lower()


def main(xlsx_path: str) -> int:
    xlsx = Path(xlsx_path)
    if not xlsx.exists():
        print(f"error: {xlsx} not found", file=sys.stderr)
        return 2

    z = zipfile.ZipFile(xlsx)
    strings = _shared_strings(z)
    sheet_to_drawing = _sheet_rel_map(z)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    unmatched_dir = OUT_DIR / "_unmatched"

    written = 0
    unmatched = 0
    for sheet_path, drawing_path in sorted(sheet_to_drawing.items()):
        rows = _rows(z, sheet_path, strings)
        if not rows:
            continue
        # Header is row 0 in the sheet model, so the first data row is index 1 and
        # the drawing anchors reference the sheet's 0-indexed row directly.
        hdr = rows[0]
        try:
            url_col = hdr.index("Source URL")
        except ValueError:
            print(f"  skipping {sheet_path}: no Source URL column", file=sys.stderr)
            continue

        row_to_image = _row_to_image(z, drawing_path)
        for row_idx, media in row_to_image.items():
            source_url = rows[row_idx][url_col] if row_idx < len(rows) and url_col < len(rows[row_idx]) else ""
            slug = _slug_from_url(source_url)
            ext = _ext_of(media)
            if slug:
                dest = OUT_DIR / slug / f"catalogue-hero{ext}"
            else:
                dest = unmatched_dir / f"{media.rsplit('/',1)[-1]}"
                unmatched += 1
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(z.read(media))
            written += 1

    print(f"wrote {written} images under {OUT_DIR.relative_to(ROOT)}")
    if unmatched:
        print(f"  {unmatched} images had no Source URL on their row (see _unmatched/)")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("usage: python tools/extract-stevensons-xlsx-images.py <xlsx>", file=sys.stderr)
        raise SystemExit(2)
    raise SystemExit(main(sys.argv[1]))
