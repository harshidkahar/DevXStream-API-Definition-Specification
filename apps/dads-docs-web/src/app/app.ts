import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

type DocsData = { application: Application; navigation: Group[]; schemas: Schema[] };
type Application = { name: string; version: string; description?: string; framework?: string; architecture?: string; servers: Server[] };
type Server = { url: string; description?: string };
type Group = { name: string; description?: string; order?: number; operations: Operation[] };
type Operation = { id: string; name: string; method: string; route: string; summary: string; description?: string; deprecated: boolean; parameters: Parameter[]; request?: RequestBody; responses: Response[]; engineering?: Record<string, string | null>; requestSnippets: Record<string, string> };
type Parameter = { name: string; in: string; type: string; required?: boolean; description?: string; example?: unknown };
type RequestBody = { schema?: string; description?: string; contentType?: string; example?: unknown };
type Response = { statusCode: string; description: string; schema?: string; contentType?: string; example?: unknown };
type Schema = { name: string; description?: string; type?: string; properties?: Record<string, SchemaProperty>; required?: string[]; enum?: unknown[]; example?: unknown };
type SchemaProperty = { type?: string; format?: string; description?: string; nullable?: boolean; example?: unknown; items?: SchemaProperty };
type Branding = { portalTitle: string; logoPath: string; primary: string; primaryDark: string };
type SearchResult = { label: string; detail: string; href: string; kind: 'operation' | 'schema' };
type TryItEnvironment = { id: string; label: string; url: string };
type TryItConfig = { enabled: boolean; allowStateChanging: boolean; environments: TryItEnvironment[]; allowedOperations: string[] };
type TryItResult = { status: number; durationMs: number; body: string };

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly http = inject(HttpClient);
  readonly docs = signal<DocsData | null>(null);
  readonly query = signal('');
  readonly dark = signal(false);
  readonly copied = signal<string | null>(null);
  readonly selectedSnippets = signal<Record<string, string>>({});
  readonly selectedResponses = signal<Record<string, string>>({});
  readonly error = signal<string | null>(null);
  readonly branding = signal<Branding>({ portalTitle: 'DevXStream DADS Documentation', logoPath: 'assets/devxstream-logo.png', primary: '#5b4cff', primaryDark: '#4030d9' });
  readonly tryIt = signal<TryItConfig>({ enabled: false, allowStateChanging: false, environments: [], allowedOperations: [] });
  readonly selectedEnvironment = signal('');
  readonly tryItBodies = signal<Record<string, string>>({});
  readonly tryItResults = signal<Record<string, TryItResult>>({});
  readonly tryItErrors = signal<Record<string, string>>({});
  readonly pendingConfirmation = signal<string | null>(null);
  readonly executing = signal<string | null>(null);
  readonly searchOpen = signal(false);
  readonly activeSearchResult = signal(0);
  readonly visibleGroups = computed(() => {
    const document = this.docs();
    if (!document) return [];
    const query = this.query().trim().toLowerCase();
    if (!query) return document.navigation;
    return document.navigation
      .map(group => ({ ...group, operations: group.operations.filter(operation => `${operation.summary} ${operation.route} ${operation.name}`.toLowerCase().includes(query)) }))
      .filter(group => group.operations.length > 0 || group.name.toLowerCase().includes(query));
  });
  readonly searchResults = computed<SearchResult[]>(() => {
    const document = this.docs();
    const query = this.query().trim().toLowerCase();
    if (!document || !query) return [];
    const operations = document.navigation.flatMap(group => group.operations
      .filter(operation => `${group.name} ${operation.name} ${operation.summary} ${operation.description} ${operation.route}`.toLowerCase().includes(query))
      .map(operation => ({ label: `${operation.method} ${operation.route}`, detail: operation.summary, href: `#${operation.id}`, kind: 'operation' as const })));
    const schemas = document.schemas.filter(schema => `${schema.name} ${schema.description} ${Object.keys(schema.properties ?? {}).join(' ')}`.toLowerCase().includes(query))
      .map(schema => ({ label: schema.name, detail: schema.description ?? 'Reusable schema', href: `#${this.modelId(schema.name)}`, kind: 'schema' as const }));
    return [...operations, ...schemas].slice(0, 12);
  });

  constructor() {
    this.http.get<DocsData>('data/ecommerce.docs.json').subscribe({
      next: (docs: DocsData) => this.docs.set(docs),
      error: () => this.error.set('The DADS documentation data could not be loaded. Run build-docs-data before building the portal.')
    });
    this.http.get<Branding>('data/branding.json').subscribe({ next: (branding: Branding) => this.branding.set(branding) });
    this.http.get<TryItConfig>('data/try-it.json').subscribe({
      next: (config: TryItConfig) => {
        this.tryIt.set(config);
        this.selectedEnvironment.set(config.environments[0]?.id ?? '');
      }
    });
  }

  methodClass(method: string): string { return `method method-${method.toLowerCase()}`; }
  stringify(value: unknown): string { return JSON.stringify(value, null, 2); }
  engineeringEntries(engineering?: Record<string, string | null>): [string, string | null][] { return Object.entries(engineering ?? {}); }
  schemaEntries(schema: Schema): [string, SchemaProperty][] { return Object.entries(schema.properties ?? {}); }
  snippetLanguage(operation: Operation): string { return this.selectedSnippets()[operation.id] ?? 'curl'; }
  selectedResponse(operation: Operation): Response | undefined { return operation.responses.find(response => response.statusCode === (this.selectedResponses()[operation.id] ?? operation.responses[0]?.statusCode)); }
  selectSnippet(operationId: string, language: string): void { this.selectedSnippets.update(value => ({ ...value, [operationId]: language })); }
  selectResponse(operationId: string, statusCode: string): void { this.selectedResponses.update(value => ({ ...value, [operationId]: statusCode })); }
  modelId(name: string): string { return `model-${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`; }
  isStateChanging(operation: Operation): boolean { return !['GET', 'HEAD', 'OPTIONS'].includes(operation.method); }
  activeEnvironment(): TryItEnvironment | undefined { return this.tryIt().environments.find(environment => environment.id === this.selectedEnvironment()); }
  canExecute(operation: Operation): boolean {
    const config = this.tryIt();
    return config.enabled && config.allowedOperations.includes(operation.id) && (!this.isStateChanging(operation) || config.allowStateChanging);
  }
  executionDisabledReason(operation: Operation): string {
    if (!this.tryIt().enabled) return 'Live requests are disabled for this documentation site.';
    if (!this.tryIt().allowedOperations.includes(operation.id)) return 'This operation is not on the environment allow-list.';
    if (this.isStateChanging(operation) && !this.tryIt().allowStateChanging) return 'State-changing requests are disabled for this environment.';
    return '';
  }
  requestUrl(operation: Operation): string {
    const environment = this.activeEnvironment();
    if (!environment) return '';
    let route = operation.route;
    for (const parameter of operation.parameters.filter(parameter => parameter.in === 'path')) route = route.replace(`{${parameter.name}}`, encodeURIComponent(String(parameter.example ?? `{${parameter.name}}`)));
    const query = operation.parameters.filter(parameter => parameter.in === 'query' && parameter.example !== undefined).map(parameter => `${encodeURIComponent(parameter.name)}=${encodeURIComponent(String(parameter.example))}`).join('&');
    return `${environment.url.replace(/\/$/, '')}${route}${query ? `?${query}` : ''}`;
  }
  requestBody(operation: Operation): string { return this.tryItBodies()[operation.id] ?? (operation.request?.example === undefined ? '' : this.stringify(operation.request.example)); }
  setRequestBody(operationId: string, body: string): void { this.tryItBodies.update(value => ({ ...value, [operationId]: body })); }
  selectEnvironment(environmentId: string): void { this.selectedEnvironment.set(environmentId); }

  async execute(operation: Operation): Promise<void> {
    if (!this.canExecute(operation)) return;
    if (this.isStateChanging(operation) && this.pendingConfirmation() !== operation.id) { this.pendingConfirmation.set(operation.id); return; }
    const environment = this.activeEnvironment();
    const url = this.requestUrl(operation);
    if (!environment || !url.startsWith(environment.url.replace(/\/$/, '') + '/')) {
      this.tryItErrors.update(value => ({ ...value, [operation.id]: 'Request blocked: URL is outside the selected environment.' }));
      return;
    }
    let body: string | undefined;
    if (operation.request) {
      body = this.requestBody(operation);
      try { if (body) JSON.parse(body); } catch { this.tryItErrors.update(value => ({ ...value, [operation.id]: 'Request body must be valid JSON.' })); return; }
    }
    this.executing.set(operation.id);
    this.tryItErrors.update(value => ({ ...value, [operation.id]: '' }));
    const started = performance.now();
    try {
      const response = await fetch(url, { method: operation.method, headers: body ? { 'Content-Type': operation.request?.contentType ?? 'application/json' } : undefined, body });
      const text = (await response.text()).slice(0, 100_000);
      this.tryItResults.update(value => ({ ...value, [operation.id]: { status: response.status, durationMs: Math.round(performance.now() - started), body: text } }));
      this.pendingConfirmation.set(null);
    } catch { this.tryItErrors.update(value => ({ ...value, [operation.id]: 'Request could not be completed. Check the selected environment and browser CORS policy.' })); }
    finally { this.executing.set(null); }
  }

  async copy(value: string, key: string): Promise<void> {
    if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(value);
    else {
      const area = document.createElement('textarea');
      area.value = value;
      area.setAttribute('readonly', '');
      area.style.position = 'fixed';
      document.body.appendChild(area);
      area.select();
      document.execCommand('copy');
      area.remove();
    }
    this.copied.set(key);
    window.setTimeout(() => this.copied.set(null), 1200);
  }

  toggleTheme(): void { this.dark.update(value => !value); }
  updateQuery(value: string): void { this.query.set(value); this.searchOpen.set(Boolean(value)); this.activeSearchResult.set(0); }
  openSearch(): void { this.searchOpen.set(this.query().length > 0); }
  moveSearchResult(offset: number): void { const results = this.searchResults(); if (!results.length) return; this.activeSearchResult.update(index => (index + offset + results.length) % results.length); }
  openActiveSearchResult(): void { const result = this.searchResults()[this.activeSearchResult()]; if (result) { window.location.hash = result.href; this.searchOpen.set(false); } }
  chooseSearchResult(result: SearchResult): void { window.location.hash = result.href; this.searchOpen.set(false); }
}
