// Illustrative catalogue for prototype purposes.
// Product and range names are Stevensons' own; PRICES ARE INVENTED and must be
// replaced with real trade terms before this is shown as anything but a mockup.

window.JT_TINT_BASES = {
  White:  { label: "White base",  upliftPerLitre: 0 },
  Pastel: { label: "Pastel base", upliftPerLitre: 6 },
  Medium: { label: "Medium base", upliftPerLitre: 14 },
  Deep:   { label: "Deep base",   upliftPerLitre: 26 },
  Accent: { label: "Accent base", upliftPerLitre: 38 }
};

window.JT_VAT_RATE = 0.15;

window.JT_PRODUCTS = [
  {
    id: "architect-premium-interior",
    brand: "Stevensons",
    range: "Architect",
    name: "Architect Premium Interior",
    blurb: "Low-VOC acrylic topcoat for interior walls and ceilings. Washable, low odour, high opacity.",
    coveragePerLitre: 8,
    coatsRecommended: 2,
    category: "Interior walls",
    rating: 4.8, reviews: 214,
    sheens: ["Matt", "Silk", "Eggshell"],
    packs: [
      { litres: 1,  priceExVat: 189.00, weightKg: 1.4 },
      { litres: 5,  priceExVat: 749.00, weightKg: 6.6 },
      { litres: 20, priceExVat: 2609.00, weightKg: 25.4 }
    ]
  },
  {
    id: "architect-roofmaster",
    brand: "Stevensons",
    range: "Architect",
    name: "Architect Roofmaster",
    blurb: "Flexible roof coating for tile, IBR and fibre cement. UV stable, resists ponding.",
    coveragePerLitre: 6,
    coatsRecommended: 2,
    category: "Roof",
    rating: 4.6, reviews: 88,
    sheens: ["Matt", "Satin"],
    packs: [
      { litres: 5,  priceExVat: 829.00, weightKg: 6.9 },
      { litres: 20, priceExVat: 2889.00, weightKg: 26.1 }
    ]
  },
  {
    id: "professional-exterior",
    brand: "Stevensons",
    range: "Professional",
    name: "Professional Exterior Acrylic",
    blurb: "Weather-resistant exterior wall coating with fungicide. Built for Highveld sun and coastal damp.",
    coveragePerLitre: 7,
    coatsRecommended: 2,
    category: "Exterior walls",
    rating: 4.7, reviews: 156,
    sheens: ["Matt", "Silk"],
    packs: [
      { litres: 1,  priceExVat: 165.00, weightKg: 1.4 },
      { litres: 5,  priceExVat: 659.00, weightKg: 6.7 },
      { litres: 20, priceExVat: 2299.00, weightKg: 25.8 }
    ]
  },
  {
    id: "tradesman-pva",
    brand: "Stevensons",
    range: "Tradesman",
    name: "Tradesman PVA",
    blurb: "Economical matt PVA for new plaster and high-volume contract work.",
    coveragePerLitre: 9,
    coatsRecommended: 2,
    category: "Interior walls",
    rating: 4.3, reviews: 312,
    sheens: ["Matt"],
    packs: [
      { litres: 5,  priceExVat: 379.00, weightKg: 6.4 },
      { litres: 20, priceExVat: 1289.00, weightKg: 24.9 }
    ]
  },
  {
    id: "durawood-sealer",
    brand: "Durawood",
    range: "Durawood",
    name: "Durawood Exterior Sealer",
    blurb: "UV-filtering timber sealer for decks, doors and window frames.",
    coveragePerLitre: 12,
    coatsRecommended: 3,
    category: "Wood care",
    rating: 4.5, reviews: 74,
    sheens: ["Satin", "Gloss"],
    packs: [
      { litres: 1, priceExVat: 249.00, weightKg: 1.1 },
      { litres: 5, priceExVat: 1019.00, weightKg: 5.3 }
    ]
  },
  {
    id: "crest-enamel",
    brand: "Crest",
    range: "Crest",
    name: "Crest Universal Enamel",
    blurb: "Solvent-based enamel for metal and wood. Hard-wearing, high gloss retention.",
    coveragePerLitre: 11,
    coatsRecommended: 2,
    category: "Enamel & metal",
    rating: 4.4, reviews: 129,
    hazard: "Flammable",
    sheens: ["Gloss", "Eggshell"],
    packs: [
      { litres: 1, priceExVat: 219.00, weightKg: 1.2 },
      { litres: 5, priceExVat: 899.00, weightKg: 5.8 }
    ]
  }
];

window.JT_SAMPLE_POT = { litres: 0.25, priceExVat: 69.00, weightKg: 0.4 };

// Sheens that change the price relative to the pack base price.
window.JT_SHEEN_UPLIFT = { Matt: 0, Silk: 0.04, Satin: 0.04, Eggshell: 0.06, Gloss: 0.08 };
