import { existsSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const source = join(root, 'src', 'app');
const output = join(root, 'dist', 'dads-docs-web', 'browser');
const checks = [];
const assert = (condition, message) => checks.push({ condition, message });
const html = readFileSync(join(source, 'app.html'), 'utf8');
const component = readFileSync(join(source, 'app.ts'), 'utf8');
const tryItConfig = JSON.parse(readFileSync(join(root, 'public', 'data', 'try-it.json'), 'utf8'));

assert(/<label class="search-label" for="docs-search">/.test(html), 'Search input has a programmatic label.');
assert(/aria-label="API documentation navigation"/.test(html), 'Navigation landmark has an accessible name.');
assert(/role="tablist"/.test(html) && /role="tab"/.test(html), 'Selectable examples and responses expose tab semantics.');
assert(/aria-pressed/.test(html) && /aria-label/.test(html), 'Theme control exposes its state and purpose.');
assert(!/\[innerHTML\]|bypassSecurityTrustHtml/.test(html + component), 'Portal never injects definition text as HTML.');
assert(/@defer \(on viewport\)/.test(html), 'Schema explorer is deferred until it approaches the viewport.');
assert(/searchResults/.test(component) && /moveSearchResult/.test(component), 'Search supports indexed results and keyboard navigation.');
assert(/canExecute\(operation\)/.test(component) && /pendingConfirmation/.test(component), 'Try-it requests use an allow-list and state-change confirmation gate.');
assert(existsSync(join(root, 'public', 'data', 'try-it.json')), 'Runtime try-it policy config is present.');
assert(tryItConfig.enabled === false && tryItConfig.allowStateChanging === false, 'Default portal policy disables live and state-changing requests.');
assert(existsSync(join(output, 'index.html')), 'Static portal output exists.');
assert(existsSync(join(output, 'data', 'branding.json')), 'Runtime branding config is present in static output.');

for (const { condition, message } of checks) console.log(`${condition ? 'PASS' : 'FAIL'} ${message}`);
if (existsSync(join(output, 'index.html'))) {
  const bytes = statSync(join(output, 'index.html')).size;
  console.log(`${bytes < 100_000 ? 'PASS' : 'FAIL'} HTML shell remains below the 100 KB transfer budget.`);
  if (bytes >= 100_000) process.exitCode = 1;
}
if (checks.some(({ condition }) => !condition)) process.exitCode = 1;
