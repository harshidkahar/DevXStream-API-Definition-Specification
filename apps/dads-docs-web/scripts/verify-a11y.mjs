import axe from 'axe-core';
import { JSDOM } from 'jsdom';

// This is the semantic shell shared by every generated page. Angular binds the
// DADS values at runtime; axe verifies that the resulting control/landmark shape
// meets WCAG 2.2 A/AA rules without depending on a developer's browser install.
const document = new JSDOM(`<!doctype html><html lang="en"><head><title>DevXStream DADS Documentation</title></head><body>
  <aside aria-label="API documentation navigation"><a href="#introduction" aria-label="DevXStream documentation home"><img alt="DevXStream — Streamline Development. Accelerate Innovation."></a>
    <label for="docs-search">Search documentation</label><input id="docs-search" type="search" role="combobox" aria-autocomplete="list" aria-expanded="false" aria-controls="search-results">
    <nav aria-label="Documentation sections"><a href="#introduction">Introduction</a><a href="#models">Models</a></nav>
  </aside>
  <main><header><p>DEVXSTREAM API REFERENCE</p><h1>DevXStream DADS Documentation</h1><button type="button" aria-label="Use dark theme" aria-pressed="false">Theme</button><a href="data/ecommerce.docs.json" download>Docs data</a></header>
  <section id="introduction"><h2>Introduction</h2><p>API documentation.</p></section><section id="models"><h2>Models</h2><p>Schema documentation.</p></section></main>
</body></html>`, { runScripts: 'dangerously' });

document.window.eval(axe.source);
const results = await document.window.axe.run(document.window.document, {
  runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'] },
  rules: { 'color-contrast': { enabled: false } }
});
for (const violation of results.violations) console.error(`${violation.id}: ${violation.help}`);
if (results.violations.length) process.exitCode = 1;
else console.log('PASS axe WCAG 2.2 A/AA semantic shell checks');
