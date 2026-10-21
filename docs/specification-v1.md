# DADS v1.0 specification

DADS is a language-neutral, versioned definition format for REST API contracts and optional DevXStream engineering metadata. The normative machine-readable schema is [dads.schema.json](../specification/v1/dads.schema.json). This page defines the field semantics for v1.0.

## Root object

| Field | Required | Meaning |
| --- | --- | --- |
| `$schema` | No | JSON Schema identifier for editor tooling. |
| `definitionVersion` | Yes | DADS document semantics version. v1 accepts the v1 compatibility baseline. |
| `application` | Yes | API identity and descriptive metadata. |
| `servers` | No | Consumer-visible base URLs. Never include credentials. |
| `modules` | Yes | Non-empty organization of endpoint operations. |
| `schemas` | No | Named reusable request/response contracts. |
| `x-*` | No | Safely ignorable extension data; it cannot alter core field meaning. |

## Application and server

`application.name` and `application.version` are required. `description`, `framework`, and `architecture` are optional human-readable metadata. Each `server` requires `url`; `description` identifies its intended environment.

## Modules and endpoints

Each module has `name`, `endpoints`, optional `description`, and optional non-negative `order`. An endpoint requires a stable `name`, uppercase HTTP `method`, slash-prefixed `route`, consumer-facing `summary`, and at least one `response`. A method/route pair must be unique in the whole definition.

`parameters` use `name`, `in` (`path`, `query`, `header`, or `cookie`), and `type`. Every route placeholder must have exactly a matching required path parameter. A request body optionally declares `schema`, `description`, `contentType`, and `example`. Responses require a valid HTTP status code (or `default`) and a description; they may also declare `schema`, `contentType`, and `example`.

## Schemas

Schemas are named reusable contracts. v1 supports `type`, object `properties`, `required` property names, `enum`, and `example`. A property supports `type`, `format`, `description`, `nullable`, `example`, and array `items`. Request/response schema references must resolve to a named DADS schema.

## Engineering metadata

`engineering` is explicitly non-contractual documentation. It may contain `cqrs`, `handler`, `authorization`, `cache`, `transaction`, `persistence`, and `validation`; it must not be used as the public API contract.

## Compatibility and limits

Unknown `x-*` values are preserved/ignored according to the [extension policy](versioning-and-extensions.md). Implementations enforce the [security and processing limits](security-and-limits.md). See the [authoring guide](authoring-guide.md) for an end-to-end example and [migration notes](migration-to-v1.md) for draft consumers.
