# DADS Versioning and Extension Policy

## Version identifiers

Every definition must have `definitionVersion`. DADS uses semantic versions after the initial `DADS 1.0-draft` period. The schema URI identifies the schema resource, while `definitionVersion` identifies the document semantics.

## Compatibility promise

- Patch releases clarify or correct validation without changing valid-document meaning.
- Minor releases add optional fields and validation rules that do not invalidate a previously valid document.
- Major releases may introduce breaking semantic or structural changes and require a new schema URI.
- Tools must reject unsupported major versions with a clear diagnostic.

## Extension mechanism

Extensions use the `x-` prefix and a registered namespace, for example `x-devxstream-codegen` or `x-contoso-rate-limit`.

- Extension values must be valid JSON.
- An extension must not redefine the meaning of a core DADS field.
- Tools must preserve unknown extensions when loading and serialising definitions.
- Tools may ignore unknown extensions, but must not silently reinterpret them.
- DADS-to-OpenAPI projection maps supported DADS-only metadata to `x-devxstream-*` fields.

## Deprecation

Deprecated fields remain supported for one major DADS version, emit a warning with a migration path, and are never reused for a different meaning.
