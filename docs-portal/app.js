const DEFINITION_URL = "../samples/ecommerce.dads.json";
let definition;

const $ = (selector, element = document) => element.querySelector(selector);
const escapeHtml = (value) => String(value ?? "").replace(/[&<>'"]/g, (char) => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", "'":"&#39;", '"':"&quot;" })[char]);

async function boot() {
  definition = await fetch(DEFINITION_URL).then((response) => {
    if (!response.ok) throw new Error("The sample DADS definition could not be loaded.");
    return response.json();
  });
  $("#api-title").textContent = definition.application.name;
  $("#download-definition").href = DEFINITION_URL;
  renderNavigation();
  renderDocument();
  wireInteractions();
}

function renderNavigation(filter = "") {
  const needle = filter.toLowerCase();
  const modules = definition.modules.map((module) => ({ ...module, endpoints: module.endpoints.filter((endpoint) => `${endpoint.name} ${endpoint.route} ${endpoint.summary}`.toLowerCase().includes(needle)) })).filter((module) => !needle || module.endpoints.length);
  $("#navigation").innerHTML = `<div class="nav-group"><h2>Overview</h2><a href="#introduction">Introduction</a></div>${modules.map((module) => `<section class="nav-group"><h2>${escapeHtml(module.name)}</h2>${module.endpoints.map((endpoint) => `<a href="#${operationId(module, endpoint)}"><span class="nav-method">${escapeHtml(endpoint.method)}</span>${escapeHtml(endpoint.route)}</a>`).join("")}</section>`).join("")}<div class="nav-group"><h2>Reference</h2><a href="#models">Models</a></div>`;
}

function renderDocument() {
  const app = definition.application;
  const overview = `<section id="introduction" class="intro"><span class="eyebrow">${escapeHtml(definition.definitionVersion)}</span><h2>${escapeHtml(app.name)}</h2><p>${escapeHtml(app.description)}</p><div class="metadata"><span class="chip">Version ${escapeHtml(app.version)}</span><span class="chip">${escapeHtml(app.architecture)}</span><span class="chip">${escapeHtml(app.framework)}</span></div><div class="server"><strong>Server</strong><br><code>${escapeHtml(definition.servers[0].url)}</code></div></section>`;
  const modules = definition.modules.map((module) => `<section id="module-${slug(module.name)}"><div class="group-heading"><span class="eyebrow">MODULE</span><h2>${escapeHtml(module.name)}</h2><p>${escapeHtml(module.description)}</p></div>${module.endpoints.map((endpoint) => operationHtml(module, endpoint)).join("")}</section>`).join("");
  const models = `<section id="models"><div class="group-heading"><span class="eyebrow">SCHEMAS</span><h2>Models</h2><p>Reusable API contracts defined in this DADS document.</p></div>${definition.schemas.map((schema) => `<article class="operation"><h2>${escapeHtml(schema.name)}</h2><p class="description">${escapeHtml(schema.description)}</p><pre><code>${escapeHtml(JSON.stringify(schema.example, null, 2))}</code></pre></article>`).join("")}</section>`;
  $("#document-content").innerHTML = overview + modules + models;
}

function operationHtml(module, endpoint) {
  const id = operationId(module, endpoint);
  const parameters = endpoint.parameters?.map((parameter) => `<span class="detail"><strong>${escapeHtml(parameter.name)}</strong> · ${escapeHtml(parameter.in)} · ${escapeHtml(parameter.type)}${parameter.required ? " · required" : ""}</span>`).join("") || "";
  const responses = endpoint.responses.map((response) => `<div class="response"><span class="status">${escapeHtml(response.statusCode)}</span><div><strong>${escapeHtml(response.description)}</strong>${response.example ? `<pre><code>${escapeHtml(JSON.stringify(response.example, null, 2))}</code></pre>` : ""}</div></div>`).join("");
  const engineering = Object.entries(endpoint.engineering ?? {}).map(([key, value]) => `<dt>${escapeHtml(key)}</dt><dd>${escapeHtml(Array.isArray(value) ? value.join(", ") : value)}</dd>`).join("");
  return `<article id="${id}" class="operation"><div class="operation-heading"><span class="method ${endpoint.method.toLowerCase()}">${escapeHtml(endpoint.method)}</span><code class="route">${escapeHtml(endpoint.route)}</code></div><h2>${escapeHtml(endpoint.summary)}</h2><p class="description">${escapeHtml(endpoint.description)}</p>${parameters ? `<div class="section-title">Parameters</div><section class="details">${parameters}</section>` : ""}${endpoint.request ? `<div class="section-title">Request body</div><pre><code>${escapeHtml(JSON.stringify(endpoint.request.example, null, 2))}</code></pre>` : ""}<section class="request-example"><div class="section-title">Request example</div><pre><code>${escapeHtml(curl(endpoint))}</code></pre><button class="copy" type="button" data-copy="${escapeHtml(curl(endpoint))}">Copy</button></section><section class="responses"><div class="section-title">Responses</div>${responses}</section>${engineering ? `<details class="engineering"><summary>DevXStream engineering details</summary><dl>${engineering}</dl></details>` : ""}</article>`;
}

function curl(endpoint) { const base = definition.servers[0].url; const request = endpoint.request ? ` \\\n+  -H 'Content-Type: application/json' \\\n+  --data '${JSON.stringify(endpoint.request.example)}'` : ""; return `curl -X ${endpoint.method} '${base}${endpoint.route}'${request}`; }
function operationId(module, endpoint) { return `${slug(module.name)}-${slug(endpoint.name)}`; }
function slug(value) { return String(value).toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, ""); }

function wireInteractions() {
  $("#search").addEventListener("input", (event) => renderNavigation(event.target.value));
  $("#theme-toggle").addEventListener("click", () => document.body.classList.toggle("dark"));
  document.addEventListener("click", async (event) => { if (!event.target.matches(".copy")) return; await navigator.clipboard.writeText(event.target.dataset.copy); event.target.textContent = "Copied"; setTimeout(() => { event.target.textContent = "Copy"; }, 1200); });
  $("#download-openapi").addEventListener("click", () => {
    const openapi = { openapi: "3.1.1", info: { title: definition.application.name, version: definition.application.version, description: definition.application.description }, servers: definition.servers, paths: Object.fromEntries(definition.modules.flatMap((module) => module.endpoints.map((endpoint) => [endpoint.route, { [endpoint.method.toLowerCase()]: { operationId: endpoint.name, summary: endpoint.summary, responses: Object.fromEntries(endpoint.responses.map((response) => [response.statusCode, { description: response.description }])) } }]))) };
    const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([JSON.stringify(openapi, null, 2)], { type: "application/json" })); link.download = "openapi.json"; link.click(); URL.revokeObjectURL(link.href);
  });
}

boot().catch((error) => { $("#document-content").innerHTML = `<p class="empty">${escapeHtml(error.message)}</p>`; });
