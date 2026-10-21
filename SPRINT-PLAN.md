# DADS Complete Development — Sprint Plan

## Planning assumptions

- Sprint length: two weeks.
- Team: one full-stack .NET developer, one Angular/frontend developer, one QA engineer shared from Sprint 2, and product/design input as needed.
- Delivery target: DADS v1.0 and a production-ready DevXStream-branded documentation portal.
- Estimates are planning points (SP), not time commitments.

## Release roadmap

| Release | Sprints | Outcome |
| --- | --- | --- |
| Foundation | 0–1 | Stable v1 scope, schema, models, parsing, and validation |
| Documentation MVP | 2–4 | DADS-driven, branded static documentation portal |
| Developer Preview | 5–6 | Search, accessibility, quality gates, CLI, OpenAPI interoperability |
| v1.0 Release | 7–8 | Hardened packages, documentation, CI/CD, and public release readiness |

## Sprint 0 — Product and technical foundation

**Status:** Complete

**Goal:** Agree on a buildable DADS v1 contract and portal design system.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Define DADS v1 boundaries | Scope statement and glossary | 3 | REST contract, engineering metadata, and out-of-scope features are explicitly separated. |
| Define versioning and extensions | Compatibility policy and `x-devxstream-*` rules | 3 | A consumer can identify the DADS version and safely ignore unknown extensions. |
| Draft JSON Schema | `dads.schema.json` draft | 8 | The sample definition validates for metadata, modules, endpoints, parameters, requests, responses, and schemas. |
| Define canonical domain model | C# model design and mapping notes | 5 | Raw JSON fields map one-to-one to versioned public contracts. |
| Create portal UX/design baseline | Wireframes and design tokens | 5 | Introduction, endpoint, schema, search, and engineering-details views are approved. |
| Establish repository standards | Solution structure, ADR template, contribution guidance | 3 | Local build and test commands are documented. |

**Sprint review:** A representative DADS file can express the planned documentation experience on paper and validates against the draft schema.

## Sprint 1 — Models, serialization, and validation

**Status:** Complete

**Goal:** Make DADS definitions safe and deterministic to load and validate.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Create models package | `DevXStream.DADS.Models` | 8 | Public models are immutable where practical and carry DADS version metadata. |
| Add JSON/YAML serialization | `DevXStream.DADS.Serialization` | 8 | Valid JSON and YAML load to equivalent canonical models. |
| Implement schema validator | Structural diagnostics | 5 | Invalid shapes report JSON pointer, rule ID, severity, and friendly message. |
| Implement semantic validator | `DevXStream.DADS.Validation` | 8 | Duplicate IDs/routes, missing references, invalid HTTP methods, and invalid response codes are detected. |
| Build core fixtures | Valid/invalid definitions | 5 | Each specification rule has at least one pass and fail fixture. |
| Add validation CLI | `dads validate` | 3 | Exit codes work in CI and diagnostics are readable in a console. |

**Sprint review:** `dads validate samples/ecommerce.dads.json` passes; intentionally broken definitions fail predictably.

## Sprint 2 — Documentation and OpenAPI projections

**Status:** Complete

**Goal:** Convert DADS into stable documentation data and interoperable OpenAPI 3.1.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Create documentation projection | `DocumentationSiteModel` | 8 | Metadata, navigation groups, operations, schemas, examples, security, and engineering details are represented without UI types. |
| Create OpenAPI projection | `DevXStream.DADS.OpenApi` | 13 | Core REST fields map to valid OpenAPI 3.1; DADS-only metadata uses documented extensions. |
| Build request snippet generator | cURL, C#, JS/TS, Python, HTTP | 8 | Snippets are generated from parameters, auth, and body examples. |
| Add contract snapshot tests | Projection fixtures | 5 | A reviewed DADS fixture generates deterministic documentation and OpenAPI output. |
| Extend CLI | `export-openapi` and `build-docs-data` | 5 | Commands write files and fail safely when definitions are invalid. |

**Sprint review:** The ECommerce sample produces a valid OpenAPI file and documentation JSON without manual transformations.

## Sprint 3 — Branded portal foundation

**Status:** Complete

**Goal:** Replace the prototype with the first production-shaped documentation UI.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Scaffold Angular portal | `apps/dads-docs-web` | 8 | Development, test, and production build commands work. |
| Apply DevXStream branding | Logo integration and design tokens | 5 | The supplied logo is used unchanged; theme values are centrally configurable. |
| Build shell/navigation | Header, sidebar, deep-linking, mobile navigation | 8 | Users can navigate introduction, module/tag, endpoint, and model pages by URL. |
| Build introduction page | Metadata, servers, downloads | 5 | API title/version/description/server environment and DADS/OpenAPI downloads render from data. |
| Add static build pipeline | Static output from a DADS file | 8 | One command builds a deployable site from the ECommerce DADS sample. |

**Sprint review:** A stakeholder can navigate a DevXStream-branded static site generated from a DADS definition.

## Sprint 4 — Endpoint and schema documentation

**Status:** Complete

**Goal:** Make endpoint pages as useful as the Scalar reference, with DADS enhancements.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Render operations | Method, route, summary, description, deprecation | 5 | Method colour is supplemental, not the sole meaning indicator. |
| Render input contracts | Path/query/header parameters and request body | 8 | Required state, constraints, example values, and schema links are visible. |
| Render response contracts | Status tabs, response schema, examples, errors | 8 | Multiple status codes and media types can be explored. |
| Build schema explorer | References, objects, arrays, enums, constraints | 8 | Cyclic references do not freeze the UI; fields link back to their source schema. |
| Build code samples | Client language selector and copy control | 5 | Samples reflect the selected server and example payload. |
| Build engineering panel | CQRS, auth, validation, persistence, generation metadata | 5 | DADS-only engineering details are clearly separated from public API contract documentation. |

**Sprint review:** An endpoint can be understood and invoked using only its generated documentation page.

## Sprint 5 — Discoverability, authoring, and CI

**Status:** Complete

**Goal:** Improve developer usability and make the project consumable in CI/CD.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Add full-text search | Indexed endpoint/schema search | 8 | Search returns relevant routes, operations, and models with keyboard navigation. |
| Add authoring guide | DADS examples and field semantics | 5 | A new user can author a basic API definition without source-code reading. |
| Add configuration/branding file | Per-site branding config | 5 | Branding can be changed without a frontend rebuild; default DevXStream logo remains untouched. |
| Create CI workflow | Build, test, validate samples, build portal | 8 | Pull requests fail for invalid definitions, failing tests, or failed static builds. |
| Add CLI integration examples | GitHub Actions/Azure DevOps samples | 3 | Teams can validate and publish docs in a pipeline. |

**Sprint review:** A new DADS project can be authored, validated, rendered, and published from CI using documentation alone.

## Sprint 6 — Accessibility, security, and developer preview

**Status:** Complete

**Goal:** Meet quality expectations before inviting early users.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Accessibility hardening | WCAG 2.2 AA review and fixes | 8 | Automated axe checks pass; all essential flows work with keyboard and screen-reader labels. |
| Secure file processing | Size/depth/reference limits and sanitisation | 8 | Malformed, oversized, cyclic, and hostile inputs cannot crash the validator or render unsafe HTML. |
| Portal performance | Code splitting and large-definition strategy | 5 | Representative documentation is interactive under the agreed performance budget. |
| End-to-end test suite | Core portal journeys | 8 | Navigation, search, downloads, deep links, themes, snippets, and schema exploration are covered. |
| Developer preview package | Prerelease SDK/CLI/site artefacts | 5 | A selected pilot can install, validate, and build the documented sample. |

**Sprint review:** Developer preview is approved with a known-issues list, feedback channel, and rollback plan.

## Sprint 7 — Live testing and interoperability (optional feature release)

**Status:** Complete

**Goal:** Safely add controlled runtime request execution and broader compatibility.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Define try-it policy | Environment and operation allow-list model | 3 | Live testing is disabled by default and safe defaults are documented. |
| Build request console | Auth-free/approved request execution UI | 8 | GET and configured operations can be executed against explicitly selected environments. |
| Add state-changing safeguards | Confirmation and visual risk cues | 5 | POST/PUT/PATCH/DELETE cannot run without explicit user action. |
| OpenAPI importer spike | Import assessment and limits | 8 | Supported fields and non-lossless conversion cases are documented with fixtures. |

**Sprint review:** Live testing remains secure by default and never exposes secrets in static site output.

## Sprint 8 — v1.0 release readiness

**Status:** Complete

**Goal:** Ship maintainable, documented DADS v1.0 packages and portal.

| Story | Deliverable | SP | Acceptance criteria |
| --- | --- | ---: | --- |
| Stabilise specification | Final schema, changelog, migration notes | 8 | Every public field has documented semantics, examples, and compatibility status. |
| Package publication pipeline | Signed NuGet prerelease/release workflow | 8 | Builds are reproducible and package metadata/license/readme are correct. |
| Documentation release | Public docs, samples, FAQ, support path | 5 | Installation, validation, portal generation, and OpenAPI export are documented end-to-end. |
| Final quality gate | Security, licence, accessibility, performance review | 8 | All release-blocking findings are resolved or formally accepted. |
| Release and monitor | v1.0 release checklist | 3 | Release artefacts, tags, release notes, and rollback instructions are complete. |

**Sprint review:** DADS v1.0 is ready to publish as an independent DevXStream developer product.

## Cross-sprint definition of done

A story is complete only when:

- Code and public contracts have appropriate automated tests.
- User-facing text is reviewed for clarity and accessibility.
- Documentation is updated with any public behaviour change.
- No secrets, environment-specific URLs, or proprietary generated output are committed.
- Validation errors are deterministic and actionable.
- Applicable accessibility and security checks pass.

## Release gates

| Gate | Required evidence |
| --- | --- |
| Documentation MVP | DADS sample produces a static, branded portal with endpoint and model pages. |
| Developer Preview | CI green, end-to-end tests green, accessibility baseline passes, prerelease packages build. |
| v1.0 | Final schema/specification approved, package pipeline verified, security/performance review passed, release notes ready. |

## Initial backlog priority

Start with Sprint 0, then Sprint 1. The existing prototype portal and ECommerce sample should be retained as design/reference artefacts but migrated to the Angular/static-build implementation in Sprint 3.
