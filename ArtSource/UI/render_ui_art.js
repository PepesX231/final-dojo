// MAMA'S COMING! UI art generator: each asset is an SVG drawn with paper / wood / metal filters and
// rendered to a transparent PNG by headless Chromium. Usage: node render.js <outDir> [only-name]
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const out = process.argv[2] || 'out'; const only = process.argv[3];
fs.mkdirSync(out, { recursive: true });

// ---------------------------------------------------------------- shared filters
const defs = (seed = 3) => `
<defs>
  <!-- paper: wobbly/torn edge + crumple lighting + fine grain -->
  <filter id="paper" x="-4%" y="-4%" width="108%" height="108%" color-interpolation-filters="sRGB">
    <feTurbulence type="fractalNoise" baseFrequency="0.035" numOctaves="3" seed="${seed}" result="warp"/>
    <feDisplacementMap in="SourceGraphic" in2="warp" scale="7" xChannelSelector="R" yChannelSelector="G" result="torn"/>
    <feTurbulence type="fractalNoise" baseFrequency="0.0065" numOctaves="5" seed="${seed + 7}" result="crumple"/>
    <feDiffuseLighting in="crumple" surfaceScale="5.5" lighting-color="#ffffff" result="light"><feDistantLight azimuth="235" elevation="62"/></feDiffuseLighting>
    <feComponentTransfer in="light" result="light2"><feFuncR type="linear" slope="0.55" intercept="0.5"/><feFuncG type="linear" slope="0.55" intercept="0.5"/><feFuncB type="linear" slope="0.55" intercept="0.5"/></feComponentTransfer>
    <feBlend in="torn" in2="light2" mode="multiply" result="lit"/>
    <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" seed="${seed + 2}" result="grain"/>
    <feColorMatrix in="grain" type="matrix" values="0 0 0 0 0.45  0 0 0 0 0.38  0 0 0 0 0.3  0 0 0 0.10 0" result="grainA"/>
    <feComposite in="grainA" in2="torn" operator="in" result="grainIn"/>
    <feMerge result="paperOut"><feMergeNode in="lit"/><feMergeNode in="grainIn"/></feMerge>
    <feComposite in="paperOut" in2="torn" operator="in"/>
  </filter>
  <filter id="softshadow" x="-10%" y="-10%" width="120%" height="125%">
    <feGaussianBlur in="SourceAlpha" stdDeviation="14"/>
    <feOffset dx="10" dy="18" result="b"/>
    <feComponentTransfer><feFuncA type="linear" slope="0.42"/></feComponentTransfer>
    <feColorMatrix type="matrix" values="0 0 0 0 0.12  0 0 0 0 0.05  0 0 0 0 0.02  0 0 0 1 0"/>
    <feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
  <filter id="wood" x="0" y="0" width="100%" height="100%" color-interpolation-filters="sRGB">
    <feTurbulence type="fractalNoise" baseFrequency="0.004 0.12" numOctaves="4" seed="${seed}" result="grain"/>
    <feColorMatrix in="grain" type="matrix" values="0 0 0 0 0.35  0 0 0 0 0.18  0 0 0 0 0.08  0 0 0 -1.3 1.05" result="streaks"/>
    <feComposite in="streaks" in2="SourceGraphic" operator="in" result="s"/>
    <feMerge><feMergeNode in="SourceGraphic"/><feMergeNode in="s"/></feMerge>
  </filter>
  <filter id="metal" x="0" y="0" width="100%" height="100%" color-interpolation-filters="sRGB">
    <feTurbulence type="fractalNoise" baseFrequency="0.012" numOctaves="4" seed="${seed + 4}" result="n"/>
    <feDiffuseLighting in="n" surfaceScale="1.6" lighting-color="#fff" result="l"><feDistantLight azimuth="225" elevation="70"/></feDiffuseLighting>
    <feComponentTransfer in="l" result="l2"><feFuncR type="linear" slope="0.4" intercept="0.62"/><feFuncG type="linear" slope="0.4" intercept="0.62"/><feFuncB type="linear" slope="0.4" intercept="0.62"/></feComponentTransfer>
    <feBlend in="SourceGraphic" in2="l2" mode="multiply" result="m"/>
    <feComposite in="m" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="grit" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="1.2" numOctaves="1" seed="${seed + 5}" result="g"/>
    <feColorMatrix in="g" type="matrix" values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.12 0" result="ga"/>
    <feComposite in="ga" in2="SourceGraphic" operator="in" result="gi"/>
    <feMerge><feMergeNode in="SourceGraphic"/><feMergeNode in="gi"/></feMerge>
  </filter>
</defs>`;

const assets = {};

// ---------------------------------------------------------------- polaroid / instant photo frame with a hole for the photo
// frame 1760x1300 at 2x; hole 1620x912 at (70,94). Padding 60 for the baked shadow.
assets.polaroid = (() => {
  const P = 60, W = 1760, H = 1300;
  return { w: W + P * 2, h: H + P * 2, svg: `
  ${defs(11)}
  <mask id="hole"><rect x="0" y="0" width="${W + P * 2}" height="${H + P * 2}" fill="#fff"/><rect x="${P + 70}" y="${P + 94}" width="1620" height="912" rx="6" fill="#000"/></mask>
  <g filter="url(#softshadow)"><g mask="url(#hole)">
    <rect x="${P}" y="${P}" width="${W}" height="${H}" rx="10" fill="#fbf7ee" filter="url(#paper)"/>
  </g></g>
  <!-- inner shadow lip around the photo -->
  <rect x="${P + 68}" y="${P + 92}" width="1624" height="916" rx="7" fill="none" stroke="#c9bfae" stroke-width="5" opacity=".8"/>
  <!-- coffee ring + pencil scribble for charm -->
  <circle cx="${P + 1560}" cy="${P + 1150}" r="88" fill="none" stroke="#b98a5c" stroke-width="9" opacity=".16"/>
  <circle cx="${P + 1566}" cy="${P + 1146}" r="80" fill="none" stroke="#b98a5c" stroke-width="3" opacity=".12"/>
  <!-- creases (a dark valley line next to a light ridge) -->
  <path d="M ${P + 1180} ${P} Q ${P + 1300} ${P + 640} ${P + 1500} ${P + H}" stroke="#6b5a40" stroke-width="3" fill="none" opacity=".16"/>
  <path d="M ${P + 1184} ${P} Q ${P + 1304} ${P + 640} ${P + 1504} ${P + H}" stroke="#fff" stroke-width="4" fill="none" opacity=".45"/>
  <path d="M ${P} ${P + 1060} Q ${P + 700} ${P + 1010} ${P + W} ${P + 1100}" stroke="#6b5a40" stroke-width="3" fill="none" opacity=".12"/>
  <path d="M ${P} ${P + 1064} Q ${P + 700} ${P + 1014} ${P + W} ${P + 1104}" stroke="#fff" stroke-width="4" fill="none" opacity=".4"/>
  <!-- folded corner, top-left -->
  <path d="M ${P} ${P + 110} L ${P + 110} ${P} L ${P} ${P} Z" fill="#000" opacity="0"/>
  ` };
})();

// ---------------------------------------------------------------- sticky note with a curled bottom-right corner
assets.sticky = (() => {
  const P = 60, W = 940, H = 1120, C = 150;
  return { w: W + P * 2, h: H + P * 2, svg: `
  ${defs(23)}
  <linearGradient id="yel" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffd25a"/><stop offset=".12" stop-color="#ffdf7a"/><stop offset="1" stop-color="#ffcf55"/></linearGradient>
  <linearGradient id="curl" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fff2b8"/><stop offset=".55" stop-color="#ffe189"/><stop offset="1" stop-color="#e9b947"/></linearGradient>
  <g filter="url(#softshadow)">
    <path d="M ${P} ${P} L ${P + W} ${P} L ${P + W} ${P + H - C} Q ${P + W - 30} ${P + H - 20} ${P + W - C} ${P + H} L ${P} ${P + H} Z" fill="url(#yel)" filter="url(#paper)"/>
  </g>
  <!-- adhesive strip shade -->
  <rect x="${P + 4}" y="${P + 4}" width="${W - 8}" height="90" fill="#e8b23c" opacity=".18"/>
  <path d="M ${P} ${P + 470} Q ${P + 470} ${P + 440} ${P + W} ${P + 500}" stroke="#8a6414" stroke-width="3" fill="none" opacity=".18"/>
  <path d="M ${P} ${P + 474} Q ${P + 470} ${P + 444} ${P + W} ${P + 504}" stroke="#fff6cf" stroke-width="4" fill="none" opacity=".55"/>
  <!-- curl: shadow under it, then the lifted flap -->
  <path d="M ${P + W} ${P + H - C} Q ${P + W - 30} ${P + H - 20} ${P + W - C} ${P + H} Q ${P + W - C + 10} ${P + H - 70} ${P + W - 60} ${P + H - C - 20} Z" fill="#7a5410" opacity=".25" transform="translate(-6,-4)"/>
  <path d="M ${P + W} ${P + H - C} Q ${P + W - 30} ${P + H - 20} ${P + W - C} ${P + H} Q ${P + W - C + 20} ${P + H - 80} ${P + W - 50} ${P + H - C - 10} Z" fill="url(#curl)"/>
  ` };
})();

// ---------------------------------------------------------------- paper tag with pinked (zigzag) edges
assets.tag = (() => {
  const P = 40, W = 660, H = 172, z = 18;
  let d = `M ${P} ${P}`;
  for (let x = P; x < P + W; x += z * 2) d += ` L ${x + z} ${P + 7} L ${Math.min(x + z * 2, P + W)} ${P}`;
  d += ` L ${P + W} ${P + H}`;
  for (let x = P + W; x > P; x -= z * 2) d += ` L ${x - z} ${P + H - 7} L ${Math.max(x - z * 2, P)} ${P + H}`;
  d += ' Z';
  return { w: W + P * 2, h: H + P * 2, svg: `
  ${defs(31)}
  <linearGradient id="org" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffc28a"/><stop offset="1" stop-color="#f7a865"/></linearGradient>
  <g filter="url(#softshadow)"><path d="${d}" fill="url(#org)" filter="url(#paper)"/></g>
  <path d="M ${P + 24} ${P + 22} L ${P + W - 24} ${P + 22}" stroke="#fff" stroke-width="3" opacity=".35" stroke-dasharray="14 10"/>
  <path d="M ${P + 24} ${P + H - 22} L ${P + W - 24} ${P + H - 22}" stroke="#fff" stroke-width="3" opacity=".35" stroke-dasharray="14 10"/>
  ` };
})();

// ---------------------------------------------------------------- masking tape strip
assets.tape = (() => {
  const W = 340, H = 104; let d = 'M 10 8';
  for (let y = 8; y < H - 8; y += 12) d += ` L ${y % 24 ? 2 : 12} ${y + 6}`;
  d += ` L 10 ${H - 8} L ${W - 10} ${H - 8}`;
  for (let y = H - 8; y > 8; y -= 12) d += ` L ${y % 24 ? W - 2 : W - 12} ${y - 6}`;
  d += ` L ${W - 10} 8 Z`;
  return { w: W, h: H, svg: `${defs(41)}<path d="${d}" fill="#efe3c4" opacity=".82" filter="url(#grit)"/>
  <path d="M 14 22 L ${W - 14} 22" stroke="#fff" stroke-width="4" opacity=".35"/>` };
})();


// ---------------------------------------------------------------- HUD: wooden frame around a metal plate (9-slice, border 96)
const bolt = (x, y, r = 16) => `<g><circle cx="${x}" cy="${y}" r="${r + 3}" fill="#3b2a1e" opacity=".35"/><circle cx="${x}" cy="${y}" r="${r}" fill="url(#boltg)"/>
  <path d="M ${x - r * .55} ${y - r * .2} L ${x + r * .55} ${y + r * .2}" stroke="#4a4f52" stroke-width="${r * .28}" stroke-linecap="round"/></g>`;
const boltDefs = `<radialGradient id="boltg" cx=".35" cy=".3" r=".8"><stop offset="0" stop-color="#f4f1ea"/><stop offset=".5" stop-color="#b9bcbc"/><stop offset="1" stop-color="#6c7072"/></radialGradient>`;
assets.woodframe = (() => {
  const W = 600, H = 600, B = 58;
  return { w: W, h: H, svg: `${defs(51)}${boltDefs}
  <linearGradient id="wd" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#c0814a"/><stop offset="1" stop-color="#94562d"/></linearGradient>
  <linearGradient id="pl" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8ea79d"/><stop offset="1" stop-color="#6d877e"/></linearGradient>
  <rect x="4" y="4" width="${W - 8}" height="${H - 8}" rx="34" fill="url(#wd)" filter="url(#wood)"/>
  <rect x="4" y="4" width="${W - 8}" height="${H - 8}" rx="34" fill="none" stroke="#5b3218" stroke-width="6"/>
  <rect x="10" y="10" width="${W - 20}" height="${H - 20}" rx="30" fill="none" stroke="#e9b27a" stroke-width="3" opacity=".55"/>
  <rect x="${B}" y="${B}" width="${W - B * 2}" height="${H - B * 2}" rx="14" fill="url(#pl)" filter="url(#metal)"/>
  <rect x="${B}" y="${B}" width="${W - B * 2}" height="${H - B * 2}" rx="14" fill="none" stroke="#3d4f49" stroke-width="5"/>
  <rect x="${B + 5}" y="${B + 5}" width="${W - B * 2 - 10}" height="${H - B * 2 - 10}" rx="11" fill="none" stroke="#c9ddd4" stroke-width="2" opacity=".6"/>
  ${bolt(B + 22, B + 22, 13)}${bolt(W - B - 22, B + 22, 13)}${bolt(B + 22, H - B - 22, 13)}${bolt(W - B - 22, H - B - 22, 13)}
  ` };
})();
// keycap "TAB"-style key (no letters; Unity draws the text)
assets.keycap = { w: 180, h: 140, svg: `${defs(61)}
  <linearGradient id="kc" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fffaf0"/><stop offset="1" stop-color="#e6dccb"/></linearGradient>
  <rect x="10" y="22" width="160" height="108" rx="22" fill="#6e5a46"/>
  <rect x="10" y="10" width="160" height="104" rx="22" fill="url(#kc)"/>
  <rect x="24" y="18" width="132" height="76" rx="16" fill="#fff" opacity=".55"/>` };
// wooden plank label (9-slice, border 56) with nail heads
assets.plank = { w: 420, h: 120, svg: `${defs(71)}
  <linearGradient id="pk" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#d39557"/><stop offset="1" stop-color="#a7663a"/></linearGradient>
  <path d="M 10 18 L 410 8 L 412 104 L 8 112 Z" fill="#4a2a16" opacity=".35" transform="translate(4,6)"/>
  <path d="M 10 18 L 410 8 L 412 104 L 8 112 Z" fill="url(#pk)" filter="url(#wood)"/>
  <path d="M 10 18 L 410 8 L 412 104 L 8 112 Z" fill="none" stroke="#5b3218" stroke-width="5" stroke-linejoin="round"/>
  <path d="M 18 28 L 400 19" stroke="#f3c48e" stroke-width="3" opacity=".5"/>
  <circle cx="30" cy="60" r="7" fill="#585c5e"/><circle cx="28" cy="58" r="3" fill="#c8cbcc"/>
  <circle cx="390" cy="56" r="7" fill="#585c5e"/><circle cx="388" cy="54" r="3" fill="#c8cbcc"/>` };
// paper slip row (9-slice, border 40)
assets.slip = (() => { const W = 400, H = 120; return { w: W, h: H, svg: `${defs(81)}
  <rect x="8" y="10" width="${W - 16}" height="${H - 20}" rx="8" fill="#fbf2df" filter="url(#paper)"/>
  <rect x="8" y="${H - 18}" width="${W - 16}" height="6" fill="#3b2a1e" opacity=".12"/>` }; })();
// gauge track + glossy fill (9-slice, border 22)
assets.gauge_track = { w: 120, h: 48, svg: `<rect x="2" y="2" width="116" height="44" rx="22" fill="#2f3b37"/><rect x="4" y="4" width="112" height="16" rx="8" fill="#000" opacity=".25"/>` };
assets.gauge_fill = { w: 120, h: 48, svg: `<linearGradient id="gf" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff"/><stop offset=".55" stop-color="#e8e8e8"/><stop offset="1" stop-color="#bdbdbd"/></linearGradient>
  <rect x="2" y="2" width="116" height="44" rx="22" fill="url(#gf)"/><rect x="12" y="8" width="96" height="10" rx="5" fill="#fff" opacity=".7"/>` };

// ---------------------------------------------------------------- render
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  for (const [name, a] of Object.entries(assets)) {
    if (only && only !== name) continue;
    await page.setViewportSize({ width: a.w, height: a.h });
    await page.setContent(`<html><body style="margin:0;background:transparent"><svg id="s" xmlns="http://www.w3.org/2000/svg" width="${a.w}" height="${a.h}" viewBox="0 0 ${a.w} ${a.h}">${a.svg}</svg></body></html>`);
    await page.locator('#s').screenshot({ path: path.join(out, 'ui_' + name + '.png'), omitBackground: true });
    console.log('rendered', name, a.w + 'x' + a.h);
  }
  await browser.close();
})();
