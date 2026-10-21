# ADR 0001: DADS is the source of truth

**Status:** Accepted

## Context

DevXStream needs a definition that can drive documentation, code generation, validation, and compatibility exports. OpenAPI cannot express all intended engineering metadata without extensions.

## Decision

DADS is the authoritative definition. OpenAPI 3.1 and the documentation site model are generated projections. Contract information remains portable; DADS-specific details use clearly named extensions when projected to OpenAPI.

## Consequences

Documentation and generators work from one canonical model. OpenAPI remains supported but cannot be treated as a lossless representation of every DADS feature.
