const DEFINITION_URL = "../samples/ecommerce.dads.json";
let definition;
const credentials = new Map(Object.entries(JSON.parse(sessionStorage.getItem("dads.credentials") || "{}")));
const $ = (selector, element = document) => element.querySelector(selector);
const esc = (value) => String(value ?? "").replace(/[&<>'"]/g, (c) => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", "'":"&#39;", '"':"&quot;" })[c]);
const pretty = (value) => value === undefined || value === null ? "" : JSON.stringify(value, null, 2);
const securitySchemes = () => definition.securitySchemes || {};
const allEndpoints = () => definition.modules.flatMap((module) => module.endpoints);

async function boot() {
  definition = await fetch(DEFINITION_URL).then((response) => {
    if (!response.ok) throw new Error("The DADS definition could not be loaded.");
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
  const modules = definition.modules.map((module) => Object.assign({}, module, { endpoints: module.endpoints.filter((endpoint) => (endpoint.name + " " + endpoint.route + " " + endpoint.summary).toLowerCase().includes(needle)) })).filter((module) => !needle || module.endpoints.length);
  $("#navigation").innerHTML = '<div class="nav-group"><h2>Overview</h2><a href="#introduction">Introduction</a></div>' + modules.map((module) => '<section class="nav-group"><h2>' + esc(module.name) + '</h2>' + module.endpoints.map((endpoint) => '<a href="#' + operationId(module, endpoint) + '"><span class="nav-method">' + esc(endpoint.method) + '</span>' + esc(endpoint.route) + '</a>').join("") + '</section>').join("") + '<div class="nav-group"><h2>Reference</h2><a href="#models">Models</a></div>';
}

function renderDocument() {
  const app = definition.application;
  const server = definition.servers?.[0]?.url || location.origin;
  const intro = '<section id="introduction" class="intro"><span class="eyebrow">' + esc(definition.definitionVersion) + '</span><h2>' + esc(app.name) + '</h2><p>' + esc(app.description) + '</p><div class="metadata"><span class="chip">Version ' + esc(app.version) + '</span><span class="chip">' + esc(app.architecture) + '</span><span class="chip">' + esc(app.framework) + '</span></div><div class="server"><strong>Server</strong><br><code>' + esc(server) + '</code></div></section>';
  const modules = definition.modules.map((module) => '<section id="module-' + slug(module.name) + '"><div class="group-heading"><span class="eyebrow">MODULE</span><h2>' + esc(module.name) + '</h2><p>' + esc(module.description) + '</p></div>' + module.endpoints.map((endpoint) => operationHtml(module, endpoint)).join("") + '</section>').join("");
  const models = '<section id="models"><div class="group-heading"><span class="eyebrow">SCHEMAS</span><h2>Models</h2><p>Reusable API contracts defined in this DADS document.</p></div>' + (definition.schemas || []).map(modelHtml).join("") + '</section>';
  $("#document-content").innerHTML = intro + modules + models;
}

function modelHtml(schema) {
  const required = new Set(schema.required || []);
  const properties = Object.entries(schema.properties || {}).map(([name, property]) => '<div class="schema-property"><div><strong>' + esc(name) + '</strong><span class="schema-type">' + esc(property.type || "object") + (property.format ? " · " + esc(property.format) : "") + '</span></div><div class="schema-description">' + esc(property.description || "") + '</div>' + (required.has(name) ? '<span class="required-badge">required</span>' : '') + '</div>').join("");
  const example = schema.example !== undefined ? '<div class="section-title">Example</div><pre><code>' + esc(pretty(schema.example)) + '</code></pre>' : "";
  return '<article class="operation schema-card"><h2>' + esc(schema.name) + '</h2><p class="description">' + esc(schema.description) + '</p>' + (properties ? '<div class="schema-properties">' + properties + '</div>' : "") + example + '</article>';
}

function operationHtml(module, endpoint) {
  const id = operationId(module, endpoint);
  const parameters = (endpoint.parameters || []).map((parameter, index) => '<label class="request-field"><span><strong>' + esc(parameter.name) + '</strong> · ' + esc(parameter.in) + ' · ' + esc(parameter.type) + (parameter.required ? ' · required' : '') + '</span><input data-parameter="' + index + '" value="' + esc(parameter.example ?? '') + '" placeholder="' + esc(parameter.description || parameter.name) + '"></label>').join("");
  const body = endpoint.request ? '<label class="request-field"><span><strong>Request body</strong> · ' + esc(endpoint.request.contentType || "application/json") + '</span><textarea data-body>' + esc(pretty(endpoint.request.example)) + '</textarea></label>' : "";
  const secured = (endpoint.security || []).map((name) => '<span class="chip security-chip">' + esc(name) + '</span>').join("");
  const responses = (endpoint.responses || []).map((response) => '<div class="response"><span class="status">' + esc(response.statusCode) + '</span><div><strong>' + esc(response.description) + '</strong>' + (response.example ? '<pre><code>' + esc(pretty(response.example)) + '</code></pre>' : '') + '</div></div>').join("");
  const contract = (parameters ? '<div class="section-title">Parameters</div><section class="request-fields">' + parameters + '</section>' : '') + body + '<section class="responses"><div class="section-title">Responses</div>' + responses + '</section>';
  const consolePanel = '<section class="client-toolbar"><label>Code sample <select class="snippet-language"><option value="curl">cURL</option><option value="javascript">JavaScript fetch</option><option value="csharp">C# HttpClient</option><option value="python">Python httpx</option></select></label><button class="send-request" type="button">Send request</button></section><pre class="code-sample"><code></code></pre><button class="copy copy-snippet" type="button">Copy code</button><section class="live-response" aria-live="polite"><div class="section-title">Response</div><div class="response-status">Ready to send a request.</div><details class="response-headers"><summary>Response headers</summary><pre><code></code></pre></details><pre class="response-body"><code></code></pre></section>';
  return '<article id="' + id + '" class="operation api-client" data-operation="' + esc(endpoint.name) + '"><div class="operation-heading"><span class="method ' + endpoint.method.toLowerCase() + '">' + esc(endpoint.method) + '</span><code class="route">' + esc(endpoint.route) + '</code>' + secured + '</div><h2>' + esc(endpoint.summary) + '</h2><p class="description">' + esc(endpoint.description) + '</p><div class="endpoint-workspace"><section class="operation-contract">' + contract + '</section><aside class="operation-console">' + consolePanel + '</aside></div></article>';
}

function requestData(card) {
  const endpoint = allEndpoints().find((item) => item.name === card.dataset.operation);
  const url = new URL(endpoint.route, definition.servers?.[0]?.url || location.origin);
  const headers = { accept: "application/json" };
  (endpoint.parameters || []).forEach((parameter, index) => {
    const value = card.querySelector('[data-parameter="' + index + '"]').value;
    if (!value) return;
    if (parameter.in === "path") url.pathname = url.pathname.replace("{" + parameter.name + "}", encodeURIComponent(value));
    if (parameter.in === "query") url.searchParams.set(parameter.name, value);
    if (parameter.in === "header") headers[parameter.name] = value;
  });
  (endpoint.security || []).forEach((name) => applyCredential(name, url, headers));
  const body = card.querySelector("[data-body]")?.value;
  if (body) headers["content-type"] = endpoint.request?.contentType || "application/json";
  return { endpoint, url, headers, body };
}

function applyCredential(name, url, headers) {
  const value = credentials.get(name);
  const scheme = securitySchemes()[name];
  if (!value || !scheme) return;
  if (scheme.type === "http") headers.authorization = (scheme.scheme?.toLowerCase() === "bearer" ? "Bearer" : scheme.scheme || "Basic") + " " + value;
  if (scheme.type === "apiKey") {
    if (scheme.in === "query") url.searchParams.set(scheme.name, value);
    else if (scheme.in === "cookie") headers.cookie = scheme.name + "=" + value;
    else headers[scheme.name] = value;
  }
}

function codeSample(data, language) {
  const headers = Object.fromEntries(Object.entries(data.headers));
  const body = data.body || "";
  if (language === "javascript") return "const response = await fetch(" + JSON.stringify(data.url.toString()) + ", {\n  method: " + JSON.stringify(data.endpoint.method) + ",\n  headers: " + pretty(headers) + (body ? ",\n  body: " + JSON.stringify(body) : "") + "\n});\nconsole.log(await response.text());";
  if (language === "csharp") return 'using var client = new HttpClient();\nusing var request = new HttpRequestMessage(HttpMethod.' + data.endpoint.method[0] + data.endpoint.method.slice(1).toLowerCase() + ', "' + data.url + '");\n' + Object.entries(headers).map(([key, value]) => 'request.Headers.TryAddWithoutValidation("' + key + '", "' + value + '");').join("\n") + (body ? '\nrequest.Content = new StringContent(' + JSON.stringify(body) + ', Encoding.UTF8, "application/json");' : "") + "\nvar response = await client.SendAsync(request);\nvar responseBody = await response.Content.ReadAsStringAsync();";
  if (language === "python") return 'import httpx\n\nresponse = httpx.request(\n    "' + data.endpoint.method + '",\n    ' + JSON.stringify(data.url.toString()) + ",\n    headers=" + pretty(headers) + (body ? ",\n    content=" + JSON.stringify(body) : "") + "\n)\nprint(response.status_code)\nprint(response.text)";
  return "curl -X " + data.endpoint.method + " '" + data.url + "'" + Object.entries(headers).map(([key, value]) => " \\\n  -H '" + key + ": " + value + "'").join("") + (body ? " \\\n  --data '" + body.replaceAll("'", "\\'") + "'" : "");
}

function refreshSnippet(card) { const data = requestData(card); card.querySelector(".code-sample code").textContent = codeSample(data, card.querySelector(".snippet-language").value); }
async function send(card) {
  const status = card.querySelector(".response-status"), responseBody = card.querySelector(".response-body code"), responseHeaders = card.querySelector(".response-headers code");
  try {
    const data = requestData(card); status.textContent = "Sending request…";
    const response = await fetch(data.url, { method: data.endpoint.method, headers: data.headers, body: data.body || undefined });
    status.textContent = response.status + " " + response.statusText;
    responseHeaders.textContent = [...response.headers].map(([key, value]) => key + ": " + value).join("\\n");
    responseBody.textContent = await response.text();
  } catch (error) { status.textContent = "Request failed: " + error.message; responseBody.textContent = ""; }
}

function renderAuthorization() {
  const entries = Object.entries(securitySchemes());
  $("#auth-fields").innerHTML = entries.map(([name, scheme]) => '<label class="request-field"><span><strong>' + esc(name) + '</strong> · ' + esc(scheme.description || (scheme.type === "apiKey" ? "API key" : (scheme.scheme || "HTTP") + " authentication")) + '</span><input data-scheme="' + esc(name) + '" type="password" autocomplete="off" value="' + esc(credentials.get(name) || "") + '" placeholder="' + esc(scheme.type === "apiKey" ? scheme.name : "Access token") + '"></label>').join("") || "<p>No DADS security schemes are defined.</p>";
}

function wireInteractions() {
  $("#search").addEventListener("input", (event) => renderNavigation(event.target.value));
  $("#theme-toggle").addEventListener("click", () => document.body.classList.toggle("dark"));
  $("#authorize").addEventListener("click", () => { renderAuthorization(); $("#auth-dialog").showModal(); });
  $("#clear-auth").addEventListener("click", () => { credentials.clear(); sessionStorage.removeItem("dads.credentials"); renderAuthorization(); });
  $("#auth-dialog").addEventListener("close", () => { $("#auth-fields").querySelectorAll("[data-scheme]").forEach((input) => input.value ? credentials.set(input.dataset.scheme, input.value) : credentials.delete(input.dataset.scheme)); sessionStorage.setItem("dads.credentials", JSON.stringify(Object.fromEntries(credentials))); document.querySelectorAll(".api-client").forEach(refreshSnippet); });
  document.querySelectorAll(".api-client").forEach((card) => {
    card.querySelectorAll("input, textarea, select").forEach((control) => control.addEventListener("input", () => refreshSnippet(card)));
    card.querySelector(".send-request").addEventListener("click", () => send(card));
    card.querySelector(".copy-snippet").addEventListener("click", async () => { await navigator.clipboard.writeText(card.querySelector(".code-sample code").textContent); const button = card.querySelector(".copy-snippet"); button.textContent = "Copied"; setTimeout(() => button.textContent = "Copy code", 1200); });
    refreshSnippet(card);
  });
  $("#download-openapi").addEventListener("click", () => {
    const openapi = { openapi: "3.1.1", info: { title: definition.application.name, version: definition.application.version }, servers: definition.servers, components: { securitySchemes: securitySchemes() }, paths: Object.fromEntries(definition.modules.flatMap((module) => module.endpoints.map((endpoint) => [endpoint.route, { [endpoint.method.toLowerCase()]: { operationId: endpoint.name, summary: endpoint.summary, security: (endpoint.security || []).map((name) => ({ [name]: [] })), responses: Object.fromEntries(endpoint.responses.map((response) => [response.statusCode, { description: response.description }])) } }]))) };
    const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([JSON.stringify(openapi, null, 2)], { type: "application/json" })); link.download = "openapi.json"; link.click(); URL.revokeObjectURL(link.href);
  });
}

function operationId(module, endpoint) { return slug(module.name) + "-" + slug(endpoint.name); }
function slug(value) { return String(value).toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, ""); }
boot().catch((error) => { $("#document-content").innerHTML = '<p class="empty">' + esc(error.message) + '</p>'; });
