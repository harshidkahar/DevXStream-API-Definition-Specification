# Contributing to DADS

## Repository conventions

- Specification changes belong in `specification/v1/` and must include a sample or fixture.
- Public semantic decisions require an ADR in `docs/adr/`.
- Samples are product documentation: keep them valid and readable.
- Do not add runtime secrets, private endpoints, or customer data.
- Use `camelCase` for DADS JSON property names and `PascalCase` for C# public types.

## Change checklist

1. Update the JSON Schema and field semantics together.
2. Add a valid fixture and, where relevant, a failing fixture.
3. Record compatibility impact in the changelog/ADR.
4. Ensure schema validation and sample parsing pass.
5. Update the authoring and portal documentation for public behaviour changes.

## Decision records

ADRs are short and immutable once accepted. Use: context, decision, consequences, and status.
