/* Just Paints prototype — shared chrome, price resolver, coverage maths, cart.
   Everything here is illustrative. The price resolver mirrors the real rule
   documented in docs/data-model.md: price is RESOLVED, never stored. */

(function () {
  "use strict";

  const VAT = window.JT_VAT_RATE;
  const BASES = window.JT_TINT_BASES;
  const SHEEN = window.JT_SHEEN_UPLIFT;

  /* ---------- money ------------------------------------------------------ */
  const round2 = (n) => Math.round((n + Number.EPSILON) * 100) / 100;

  function zar(n) {
    const v = round2(n).toFixed(2).split(".");
    const int = v[0].replace(/\B(?=(\d{3})+(?!\d))/g, " ");
    return "R " + int + "," + v[1];
  }

  /* ---------- the price resolver ----------------------------------------- */
  /* priceExVat = pack price + sheen uplift + (base uplift/L x litres)        */
  function resolvePrice(product, pack, sheen, colour) {
    const baseName = colour ? colour.tintBase : "White";
    const tint = BASES[baseName] || BASES.White;
    const packPrice = pack.priceExVat;
    const sheenUplift = round2(packPrice * (SHEEN[sheen] || 0));
    const tintUplift = round2(tint.upliftPerLitre * pack.litres);
    const exVat = packPrice + sheenUplift + tintUplift;
    return {
      packPrice, sheenUplift, tintUplift,
      tintBase: baseName,
      tintBaseLabel: tint.label,
      exVat: round2(exVat),
      incVat: round2(exVat * (1 + VAT)),   // round once, at the end
      vat: round2(exVat * VAT)
    };
  }

  /** Cheapest inclusive price across a product's packs, for "from" labels. */
  function fromPrice(product, colour) {
    const sheen = product.sheens[0];
    return Math.min(...product.packs.map(p => resolvePrice(product, p, sheen, colour).incVat));
  }

  /* ---------- coverage calculator ---------------------------------------- */
  function litresNeeded(areaM2, coats, coveragePerLitre) {
    if (!areaM2 || !coveragePerLitre) return 0;
    return round2((areaM2 * coats) / coveragePerLitre);
  }

  function bestPackCombination(product, litres, sheen, colour) {
    if (litres <= 0) return null;
    const packs = product.packs.slice().sort((a, b) => b.litres - a.litres);
    const priced = packs.map(p => ({
      pack: p,
      litres: p.litres,
      cost: resolvePrice(product, p, sheen, colour).incVat
    }));

    let best = null;
    const maxOf = (i) => Math.ceil(litres / priced[i].litres) + 1;

    const walk = (i, counts, litresSoFar, costSoFar) => {
      if (costSoFar > (best ? best.cost : Infinity)) return;
      if (i === priced.length) {
        if (litresSoFar >= litres) {
          if (!best || costSoFar < best.cost ||
             (costSoFar === best.cost && litresSoFar < best.litres)) {
            best = { counts: counts.slice(), cost: round2(costSoFar), litres: round2(litresSoFar) };
          }
        }
        return;
      }
      const cap = maxOf(i);
      for (let n = 0; n <= cap; n++) {
        counts[i] = n;
        walk(i + 1, counts, litresSoFar + n * priced[i].litres, costSoFar + n * priced[i].cost);
      }
      counts[i] = 0;
    };
    walk(0, new Array(priced.length).fill(0), 0, 0);

    if (!best) return null;
    return {
      cost: best.cost,
      litres: best.litres,
      surplus: round2(best.litres - litres),
      lines: priced
        .map((p, i) => ({ pack: p.pack, qty: best.counts[i], each: p.cost }))
        .filter(l => l.qty > 0)
    };
  }

  /* ---------- paint tin artwork ------------------------------------------ */
  /* Products have no photography yet, so every tin is drawn. It doubles as
     the colour preview on the product page: the lid and the label stripe
     carry whatever colour the customer has chosen. */
  function tinSvg(hex, opts) {
    const o = opts || {};
    const colour = hex || "#e9eaec";
    const ink = contrastOn(colour);
    const label = o.label || "just paints";
    const sub = o.sub || "";
    return `
<svg class="tin-art" viewBox="0 0 160 180" role="img" aria-label="${label} tin">
  <defs>
    <linearGradient id="g-body" x1="0" x2="1">
      <stop offset="0"    stop-color="#d7dae0"/>
      <stop offset="0.18" stop-color="#ffffff"/>
      <stop offset="0.55" stop-color="#f2f3f5"/>
      <stop offset="1"    stop-color="#c9ccd3"/>
    </linearGradient>
    <linearGradient id="g-lid" x1="0" x2="1">
      <stop offset="0"    stop-color="${colour}" stop-opacity="0.82"/>
      <stop offset="0.35" stop-color="${colour}"/>
      <stop offset="1"    stop-color="${colour}" stop-opacity="0.72"/>
    </linearGradient>
  </defs>
  <path d="M34 34 Q80 6 126 34" fill="none" stroke="#9aa0aa" stroke-width="4" stroke-linecap="round"/>
  <rect x="28" y="40" width="104" height="122" rx="7" fill="url(#g-body)" stroke="#b6bac2"/>
  <ellipse cx="80" cy="40" rx="52" ry="12" fill="url(#g-lid)" stroke="#9aa0aa"/>
  <ellipse cx="80" cy="40" rx="38" ry="8" fill="${colour}" opacity="0.55"/>
  <rect x="28" y="78" width="104" height="52" fill="#ffffff" opacity="0.95"/>
  <rect x="28" y="78" width="104" height="5" fill="${colour}"/>
  <text x="80" y="100" text-anchor="middle" class="tin-label">${label}</text>
  ${sub ? `<text x="80" y="118" text-anchor="middle" class="tin-sub">${sub}</text>` : ""}
  <rect x="28" y="126" width="104" height="4" fill="${colour}" opacity="0.7"/>
  <ellipse cx="80" cy="162" rx="52" ry="9" fill="#c9ccd3" opacity="0.5"/>
</svg>`;
  }

  /* ---------- product card ------------------------------------------------ */
  function productCard(product, colour) {
    const from = fromPrice(product, colour);
    const sizes = product.packs.map(p => `${p.litres}L`).join(" · ");
    const href = `product.html?p=${product.id}${colour ? `&colour=${colour.code}` : ""}`;
    return `
<a class="product-card" href="${href}">
  <div class="product-art">
    ${tinSvg(colour ? colour.hex : null, { label: product.range, sub: product.sheens[0] })}
    ${product.hazard ? `<span class="badge badge-warn art-flag">${product.hazard}</span>` : ""}
  </div>
  <div class="product-body">
    <span class="product-brand">${product.brand} &middot; ${product.category}</span>
    <h3>${product.name}</h3>
    <p class="product-blurb">${product.blurb}</p>
    <div class="product-meta">
      <span>&#9733; ${product.rating} <span class="muted">(${product.reviews})</span></span>
      <span class="muted">${product.coveragePerLitre} m&sup2;/L</span>
    </div>
    <div class="product-sizes muted">${sizes}</div>
    <div class="product-price">
      <strong>from ${zar(from)}</strong>
      <span class="muted xs">incl. VAT${colour ? ` &middot; in ${colour.name}` : ""}</span>
    </div>
  </div>
</a>`;
  }

  /* ---------- cart -------------------------------------------------------- */
  const CART_KEY = "jt.paints.cart.v1";

  function readCart() {
    try { return JSON.parse(localStorage.getItem(CART_KEY)) || []; }
    catch (e) { return []; }
  }
  function writeCart(lines) {
    try { localStorage.setItem(CART_KEY, JSON.stringify(lines)); } catch (e) { /* private mode */ }
    renderCart();
  }
  function addToCart(line) {
    const lines = readCart();
    const key = l => [l.productId, l.sheen, l.litres, l.colourCode || "-"].join("|");
    const found = lines.find(l => key(l) === key(line));
    if (found) found.qty += line.qty; else lines.push(line);
    writeCart(lines);
    openCart();
  }
  function cartCount() { return readCart().reduce((n, l) => n + l.qty, 0); }

  /* ---------- chrome ------------------------------------------------------ */
  /* Just Paints is its own storefront with its own identity. Just Things is
     the parent marketplace, present as a way back up, not as a prefix. */
  const NAV = [
    ["index.html", "Home"],
    ["products.html", "All paint"],
    ["products.html?category=Interior+walls", "Interior"],
    ["products.html?category=Exterior+walls", "Exterior"],
    ["products.html?category=Roof", "Roof"],
    ["products.html?category=Wood+care", "Wood care"],
    ["products.html?category=Enamel+%26+metal", "Enamel & metal"],
    ["colours.html", "Colours"],
  ];

  const ROLLER_TILE = `
<svg viewBox="0 0 48 48" aria-hidden="true" focusable="false">
  <rect width="48" height="48" rx="3" fill="var(--jt-gold-400)"/>
  <g fill="none" stroke="#fff" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round">
    <rect x="9" y="11" width="22" height="9" rx="2" transform="rotate(-18 20 15.5)"/>
    <path d="M30 23 l4 4 -7 6"/>
    <path d="M25 35 l-3 3"/>
  </g>
</svg>`;

  function header(active) {
    return `
<div class="proto-note">
  Prototype &middot; colour names, codes and hex are Stevensons&rsquo; published Real Colours.
  <strong>Prices, stock and delivery are illustrative.</strong>
</div>
<div class="utility"><div class="wrap">
  <a class="parent-link" href="#" title="Just Things marketplace">
    <svg viewBox="0 0 30 12" aria-hidden="true" focusable="false">
      <rect x="0"  width="8" height="12" rx="1" fill="var(--jt-gold-400)"/>
      <rect x="11" width="8" height="12" rx="1" fill="var(--jt-azure-400)"/>
      <rect x="22" width="8" height="12" rx="1" fill="var(--jt-gold-400)"/>
    </svg>
    Just Things
  </a>
  <span class="utility-divider" aria-hidden="true"></span>
  <span>Deliver to <strong>2196</strong></span>
  <span class="promo">
    <span>Tinted to order in 24 hours</span>
    <span>Free delivery over R1 500</span>
  </span>
  <span class="spacer"></span>
  <a href="#">Help</a><a href="#">Track order</a><a href="#">Trade accounts</a>
</div></div>
<header class="masthead"><div class="wrap">
  <a class="brand" href="index.html" aria-label="Just Paints home">
    <span class="brand-tile">${ROLLER_TILE}</span>
    <span class="brand-word"><span>just</span><span>paints</span></span>
  </a>
  <form class="searchbar" role="search" onsubmit="return false;">
    <input type="search" placeholder="Search paint, colour name or RC code&hellip;" aria-label="Search">
    <button type="submit">Search</button>
  </form>
  <div class="masthead-actions">
    <a class="act" href="#"><span aria-hidden="true">&#9787;</span><span><strong>My account</strong>Sign in</span></a>
    <button class="act" type="button" onclick="JT.openCart()">
      <span aria-hidden="true">&#128722;</span>
      <span><strong>Cart</strong><span id="cart-total">R 0,00</span></span>
      <span class="pill" id="cart-count">0</span>
    </button>
  </div>
</div></header>
<nav class="mainnav"><div class="wrap">
  ${NAV.map(([h, l]) => `<a href="${h}"${l === active ? ' aria-current="page"' : ""}>${l}</a>`).join("")}
</div></nav>`;
  }

  function footer() {
    return `
<footer class="site"><div class="wrap">
  <div class="cols">
    <div>
      <h4>Shop</h4>
      <a href="products.html">All paint</a>
      <a href="colours.html">Browse colours</a>
      <a href="#">Sample pots</a>
      <a href="#">Trade accounts</a>
    </div>
    <div>
      <h4>Help</h4>
      <a href="#">Delivery &amp; lead times</a>
      <a href="#">Returns policy</a>
      <a href="#">Colour accuracy</a>
      <a href="#">Contact us</a>
    </div>
    <div>
      <h4>About</h4>
      <a href="#">About Just Paints</a>
      <a href="#">Our manufacturers</a>
      <a href="#">Painting guides</a>
    </div>
    <div>
      <h4>Just Things</h4>
      <p class="xs" style="color:#aeb6d4;margin:0 0 var(--jt-space-2)">
        Just Paints is part of Just Things, the marketplace for tools, hardware
        and home improvement.
      </p>
      <a href="#">Visit Just Things &rarr;</a>
      <a href="#">Sell on Just Things</a>
    </div>
  </div>
  <div class="legal">
    Prices include VAT at 15%. Custom-tinted paint is mixed to order and cannot be
    returned under the ECTA cooling-off right, except where it does not match the
    specification ordered.
    <br>Prototype &mdash; not a live store.
  </div>
</div></footer>`;
  }

  function cartDrawer() {
    return `
<div id="cart-backdrop" hidden></div>
<aside id="cart-drawer" hidden aria-label="Basket">
  <div class="cart-head">
    <h3>Your basket</h3>
    <button type="button" class="cart-close" onclick="JT.closeCart()" aria-label="Close basket">&times;</button>
  </div>
  <div id="cart-body"></div>
  <div class="cart-foot">
    <div class="row" style="justify-content:space-between">
      <span>Subtotal <span class="xs muted">incl. VAT</span></span>
      <strong id="cart-subtotal">R 0,00</strong>
    </div>
    <p class="xs muted" id="cart-weight"></p>
    <button class="btn btn-primary btn-block btn-lg" type="button">Checkout</button>
  </div>
</aside>`;
  }

  function renderCart() {
    const lines = readCart();
    const body = document.getElementById("cart-body");
    const count = document.getElementById("cart-count");
    const total = document.getElementById("cart-total");
    if (!body) return;

    const subtotal = lines.reduce((s, l) => s + l.incVat * l.qty, 0);
    const weight = lines.reduce((s, l) => s + (l.weightKg || 0) * l.qty, 0);

    if (count) count.textContent = cartCount();
    if (total) total.textContent = zar(subtotal);
    document.getElementById("cart-subtotal").textContent = zar(subtotal);
    document.getElementById("cart-weight").textContent = weight
      ? `Basket weight ${round2(weight)} kg — courier rated on weight, not a flat fee.`
      : "";

    if (!lines.length) {
      body.innerHTML = `<p class="muted small" style="padding:var(--jt-space-6)">Your basket is empty.</p>`;
      return;
    }

    body.innerHTML = lines.map((l, i) => `
      <div class="cart-line">
        <div class="cart-sw" style="background:${l.colourHex || "#f4f4f4"}"></div>
        <div style="flex:1;min-width:0">
          <strong class="small">${l.name}</strong>
          <div class="xs muted">${l.sheen} &middot; ${l.litres}L${l.colourCode ? ` &middot; ${l.colourName} ${l.colourCode}` : " &middot; Factory white"}</div>
          ${l.madeToOrder
            ? `<span class="badge badge-warn" style="margin-top:4px">Mixed to order &mdash; non-returnable</span>`
            : `<span class="badge badge-ok" style="margin-top:4px">Returnable within 7 days</span>`}
          <div class="xs muted" style="margin-top:4px">${l.tintBaseLabel}</div>
        </div>
        <div style="text-align:right">
          <strong class="small">${zar(l.incVat * l.qty)}</strong>
          <div class="xs muted">${l.qty} &times; ${zar(l.incVat)}</div>
          <button class="linkish xs" type="button" onclick="JT.removeLine(${i})">Remove</button>
        </div>
      </div>`).join("");
  }

  function openCart() {
    document.getElementById("cart-drawer").hidden = false;
    document.getElementById("cart-backdrop").hidden = false;
    document.body.style.overflow = "hidden";
  }
  function closeCart() {
    document.getElementById("cart-drawer").hidden = true;
    document.getElementById("cart-backdrop").hidden = true;
    document.body.style.overflow = "";
  }
  function removeLine(i) {
    const lines = readCart();
    lines.splice(i, 1);
    writeCart(lines);
  }

  function mount(activeNav) {
    document.getElementById("jt-header").innerHTML = header(activeNav);
    document.getElementById("jt-footer").innerHTML = footer() + cartDrawer();
    document.getElementById("cart-backdrop").addEventListener("click", closeCart);
    document.addEventListener("keydown", e => { if (e.key === "Escape") closeCart(); });
    renderCart();
  }

  /* ---------- helpers ----------------------------------------------------- */
  const products = () => window.JT_PRODUCTS;
  const productById = (id) => window.JT_PRODUCTS.find(p => p.id === id);
  const categories = () => [...new Set(window.JT_PRODUCTS.map(p => p.category))];
  const colours = () => window.JT_COLOURS;
  const colourByCode = (c) => window.JT_COLOURS.find(x => x.code === c);
  const param = (k) => new URLSearchParams(location.search).get(k);

  function contrastOn(hex) {
    const r = parseInt(hex.substr(1, 2), 16), g = parseInt(hex.substr(3, 2), 16), b = parseInt(hex.substr(5, 2), 16);
    const f = (c) => { c /= 255; return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4); };
    return (0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b)) > 0.42 ? "#14171b" : "#ffffff";
  }

  window.JT = {
    zar, round2, resolvePrice, fromPrice, litresNeeded, bestPackCombination,
    tinSvg, productCard,
    readCart, addToCart, removeLine, openCart, closeCart, renderCart, mount,
    products, productById, categories, colours, colourByCode, param, contrastOn,
    VAT, BASES
  };
})();
