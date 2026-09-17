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

  function money(n) {
    return "R " + round2(n).toFixed(2)
      .replace(".", ",")
      .replace(/\B(?=(\d{3})+(?!\d))/g, " ");
  }
  // Fix: group only the integer part.
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

  /* ---------- coverage calculator ---------------------------------------- */
  /* Returns the cheapest combination of pack sizes covering the litres needed. */
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
  function header(active) {
    const nav = [
      ["index.html", "Home"],
      ["colours.html", "Colours"],
      ["product.html", "Interior"],
      ["product.html?p=professional-exterior", "Exterior"],
      ["product.html?p=architect-roofmaster", "Roof"],
      ["product.html?p=durawood-sealer", "Wood care"],
      ["product.html?p=crest-enamel", "Enamel & metal"]
    ];
    return `
<div class="proto-note">
  Prototype &middot; colour names, codes and hex are Stevensons' published Real Colours.
  <strong>Prices, stock and delivery are illustrative.</strong>
</div>
<div class="utility"><div class="wrap">
  <span>Deliver to <strong>2196</strong></span>
  <span class="promo">
    <span>Tinted to order in 24 hours</span>
    <span>Free delivery over R1 500</span>
    <span>Trade accounts welcome</span>
  </span>
  <span class="spacer"></span>
  <a href="#">Help</a><a href="#">Track order</a><a href="#">Sell on Just Things</a>
</div></div>
<header class="masthead"><div class="wrap">
  <a class="brand" href="index.html">
    <img src="assets/logo.png" alt="Just Things">
    <span class="vertical">Paints</span>
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
  ${nav.map(([h, l]) => `<a href="${h}"${l === active ? ' aria-current="page"' : ""}>${l}</a>`).join("")}
</div></nav>`;
  }

  function footer() {
    return `
<footer class="site"><div class="wrap">
  <div class="cols">
    <div>
      <h4>Just Paints</h4>
      <a href="colours.html">Browse colours</a>
      <a href="#">Coverage calculator</a>
      <a href="#">Order sample pots</a>
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
      <a href="#">About Just Things</a>
      <a href="#">Our manufacturers</a>
      <a href="#">Sell on Just Things</a>
    </div>
    <div>
      <h4>Legal</h4>
      <a href="#">Terms &amp; conditions</a>
      <a href="#">Privacy &amp; POPIA</a>
      <a href="#">Consumer rights</a>
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
  const colours = () => window.JT_COLOURS;
  const colourByCode = (c) => window.JT_COLOURS.find(x => x.code === c);
  const param = (k) => new URLSearchParams(location.search).get(k);

  function contrastOn(hex) {
    const r = parseInt(hex.substr(1, 2), 16), g = parseInt(hex.substr(3, 2), 16), b = parseInt(hex.substr(5, 2), 16);
    const f = (c) => { c /= 255; return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4); };
    return (0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b)) > 0.42 ? "#14171b" : "#ffffff";
  }

  window.JT = {
    zar, money, round2, resolvePrice, litresNeeded, bestPackCombination,
    readCart, addToCart, removeLine, openCart, closeCart, renderCart, mount,
    products, productById, colours, colourByCode, param, contrastOn, VAT, BASES
  };
})();
