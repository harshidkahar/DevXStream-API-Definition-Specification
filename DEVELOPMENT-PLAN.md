# DADS Branded API Documentation Portal — Development Plan

## 1. Objective

Build a DevXStream-branded documentation portal that renders API documentation from the DevXStream API Definition Specification (DADS). It must provide a polished API-reference experience comparable to Scalar while exposing DADS capabilities that do not exist in OpenAPI, including architecture, CQRS, validation, persistence, and code-generation metadata.

The portal will treat DADS as the source of truth. It will optionally produce OpenAPI 3.1 documents so existing client generators, gateways, and documentation tools remain compatible.

## 2. Scope for version 1

### In scope

- DevXStream-branded, responsive web documentation UI.
- DADS v1 parser and semantic validator.
- Documentation model derived from a DADS definition.
- Application overview: title, version, description, environments/servers, contacts, licence, and links.
- Left navigation grouped by modules and/or tags.
- Endpoint pages: method, route, summary, description, authentication, parameters, request body, responses, schemas, examples, and errors.
- Generated cURL, C#, JavaScript/TypeScript, Python, and HTTP request snippets.
- Deep links to modules, endpoints, schemas, and fields.
- Search across endpoints, schemas, and descriptions.
- Download of the source DADS document and generated OpenAPI 3.1 document.
- Static-site build and local hosting mode.
- Dark/light themes and accessible keyboard navigation.

### Deferred

- Live API testing against arbitrary remote servers.
- User accounts, team workspaces, editing, and collaboration.
- Visual API designer.
- OpenAPI import user interface (the adapter can be developed separately).
- Generated SDK publishing.

## 3. Product architecture

```text
DADS JSON / YAML
       |
       v
Parser + JSON Schema validation + semantic validation
       |
       v
Canonical DADS domain model
       |-------------------------|
       v                         v
Documentation projection    OpenAPI 3.1 projection
       |                         |
       v                         v
Branded Docs Portal       Download / external tooling
```

The documentation portal must consume the canonical DADS domain model rather than read raw JSON directly. This preserves a stable UI even as DADS gains additional syntaxes, importers, or format versions.

## 4. Suggested solution structure

```text
DevXStream.DADS/
├── specification/
│   └── v1/dads.schema.json
├── src/
│   ├── DevXStream.DADS.Models/          # Versioned domain contracts
│   ├── DevXStream.DADS.Serialization/   # JSON/YAML loading and writing
│   ├── DevXStream.DADS.Validation/      # Schema and semantic rules
│   ├── DevXStream.DADS.OpenApi/          # DADS → OpenAPI 3.1 projection
│   ├── DevXStream.DADS.Docs/             # Documentation projection and site build API
│   └── DevXStream.DADS.Cli/              # validate, build-docs, export-openapi
├── apps/
│   └── dads-docs-web/                    # Angular documentation frontend
├── samples/
│   └── ecommerce.dads.json
├── tests/
│   ├── DevXStream.DADS.Validation.Tests/
│   ├── DevXStream.DADS.OpenApi.Tests/
│   ├── DevXStream.DADS.Docs.Tests/
│   └── dads-docs-web-e2e/
└── docs/
    ├── specification.md
    ├── authoring-guide.md
    └── documentation-portal.md
```

## 5. Documentation model

Create an explicit `DocumentationSiteModel` projection with no UI-library types. It should include:

- `Site`: branding, theme, logo, favicon, custom CSS token overrides, footer links.
- `Application`: name, version, description, terms/licence, contacts, server environments.
- `NavigationGroup`: label, description, order, module/tag identity, operations.
- `Operation`: method, route, summary, description, operation ID, security, deprecation state, parameters, request bodies, responses, examples, and extensions.
- `Schema`: reusable types, properties, enums, constraints, references, and examples.
- `DadsEngineeringDetails`: CQRS type, entity mapping, persistence operation, transaction policy, validation rules, authorization policy, generation metadata.

The UI should display standard contract details first and offer an “Engineering details” panel for DADS-specific information. This keeps the primary endpoint page familiar to API consumers.

## 6. User experience design

### Primary layout

- **Header:** DevXStream/DADS logo, API name and version, search, theme control, document downloads.
- **Sidebar:** Introduction, grouped modules/tags, operations, models, and DADS engineering sections.
- **Content area:** Readable endpoint documentation with method badge and copyable route.
- **Code panel:** Language selector and copy action for generated request snippets.
- **Response panel:** Status tabs, sample payload, schema inspector, and error cases.

### Visual branding

Define branding through design tokens instead of hard-coded styles: primary/secondary colours, semantic method colours, typography, spacing, radius, shadows, logo assets, and theme modes. This allows white-label documentation later without forking the frontend.

### Accessibility requirements

- WCAG 2.2 AA colour contrast and keyboard navigation.
- Semantic headings and landmarks.
- Visible focus states.
- Accessible labels for copy, expand/collapse, search, theme, and language selectors.
- No information conveyed by colour alone.

## 7. Milestones

### Milestone 0 — Foundation and decisions (1 week)

Deliverables:

- DADS v1 scope and terminology agreed.
- JSON Schema draft covering documentation-relevant fields.
- Branding token set and wireframes for introduction, endpoint, schema, and engineering-detail views.
- Architecture decision records for Angular, static-generation strategy, and DADS-to-OpenAPI compatibility.

Exit criteria:

- A sample `ecommerce.dads.json` can represent at least five endpoints, schemas, validation, authentication, and one DADS engineering extension.

### Milestone 1 — DADS core libraries (2 weeks)

Deliverables:

- Strongly typed models.
- JSON/YAML serializer.
- JSON Schema validation and semantic validation.
- Stable diagnostics with file path, JSON pointer, severity, rule ID, and human-readable message.
- CLI command: `dads validate <file>`.

Exit criteria:

- Valid and invalid sample definitions have deterministic test coverage.

### Milestone 2 — Documentation projection and OpenAPI export (2 weeks)

Deliverables:

- `DocumentationSiteModel` projection.
- DADS-to-OpenAPI 3.1 exporter for REST contract fields.
- Explicit mapping of non-portable DADS fields to `x-devxstream-*` extensions.
- CLI commands: `dads export-openapi` and `dads build-docs-data`.

Exit criteria:

- Generated OpenAPI is valid and opens in a standard OpenAPI viewer.
- Documentation model captures every v1 public documentation field.

### Milestone 3 — Branded portal MVP (3 weeks)

Deliverables:

- Angular app shell, DevXStream theme, responsive layout, and dark/light modes.
- Introduction page and grouped navigation.
- Endpoint pages with request/response/schema/example rendering.
- Code snippets for cURL, C#, JavaScript/TypeScript, Python, and HTTP.
- DADS/OpenAPI downloads and deep linking.

Exit criteria:

- The ECommerce sample renders as a complete, navigable static site.
- Core page load is under 2.5 seconds on a production build and normal broadband profile.

### Milestone 4 — Developer experience and quality (2 weeks)

Deliverables:

- Full-text search.
- Copy actions, collapsible schema tree, generated error examples.
- DADS engineering-details panel.
- Accessibility audit and end-to-end tests.
- Hosting/deployment guidance.

Exit criteria:

- Keyboard-only navigation works for all core flows.
- Automated tests cover parsing, validation, projections, snippets, deep links, and critical UI flows.

### Milestone 5 — Controlled live testing (optional, 2 weeks)

Deliverables:

- Per-environment “Try it” configuration.
- Explicit allow-listing of safe operations and confirmation for state-changing operations.
- Auth configuration that never embeds secrets in static output.

Exit criteria:

- Live testing is disabled by default and cannot execute unsafe calls without explicit configuration.

## 8. API and CLI design

Initial CLI surface:

```text
dads validate ecommerce.dads.json
dads export-openapi ecommerce.dads.json --output openapi.json
dads build-docs ecommerce.dads.json --output ./dist/docs
dads serve ecommerce.dads.json --port 8080
```

Library consumers should be able to load, validate, project, and export without invoking the CLI.

## 9. Quality strategy

- Unit tests: type mapping, validation rules, OpenAPI export, request-snippet generation.
- Conformance tests: valid/invalid DADS fixtures for every specification rule.
- Snapshot tests: documentation-model and OpenAPI output for stable samples.
- UI component tests: rendering of parameters, schemas, examples, and errors.
- End-to-end tests: navigation, search, copy controls, theme switching, downloads, and deep links.
- Accessibility tests: automated axe checks plus keyboard review.
- Security tests: invalid references, oversized documents, cyclic schemas, untrusted examples, and HTML sanitisation.

## 10. Release plan

1. Publish DADS v1 JSON Schema and specification draft.
2. Release .NET packages as prerelease versions.
3. Publish the portal as a static demonstration site using the ECommerce sample.
4. Run a small developer preview with DevXStream consumers.
5. Stabilise the schema, publish v1.0 packages, and document compatibility commitments.

Version the DADS specification independently from the SDK and portal. Use semantic versioning for packages, and support a schema-version migration strategy before making breaking changes.

## 11. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| DADS mixes public contract and implementation details | Keep the contract and engineering sections separate in both the model and UI. |
| OpenAPI conversion loses information | Document conversion boundaries and use `x-devxstream-*` extensions for DADS-only metadata. |
| Documentation becomes slow for large definitions | Build a static indexed model; lazy-load groups and schemas. |
| Unsafe live requests | Disable live testing by default; use explicit environment and operation allow-lists. |
| Branding later requires a rebuild | Drive all visual choices through a versioned design-token configuration. |

## 12. Definition of done for v1

V1 is complete when a developer can write a valid DADS file, run one command, and receive a DevXStream-branded static documentation site that accurately documents the application, grouped operations, endpoint contracts, reusable models, examples, validation, security, and DADS engineering details; download a compatible OpenAPI 3.1 document; and validate the source definition in CI.
