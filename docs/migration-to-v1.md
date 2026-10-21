# Migrating DADS draft definitions to v1.0

The v1.0 toolchain accepts `DADS 1.0-draft` documents as its compatibility baseline. Existing valid draft documents therefore continue to validate, project to OpenAPI, and build portal data without a mechanical rewrite.

Before publishing a definition as v1.0:

1. Validate it with the released CLI.
2. Ensure every route placeholder has a required path parameter.
3. Move consumer-visible behavior into endpoints, requests, responses, and schemas; retain implementation-only detail under `engineering`.
4. Replace any organization-specific unprefixed fields with `x-organization-*` extensions.
5. Review `servers` and examples for credentials, private hostnames, and personal data.

No v1.0 fields are removed or renamed. Future breaking changes will use a new schema URI and a new DADS major version.
