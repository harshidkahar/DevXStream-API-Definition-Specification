# DADS to OpenAPI 3.1 Projection

The exporter produces a deterministic OpenAPI 3.1.1 document from a valid DADS definition.

| DADS | OpenAPI |
| --- | --- |
| `application` | `info` |
| `servers` | `servers` |
| `modules` | `tags` |
| module endpoints | `paths` operations |
| parameters, request, responses | Standard operation fields |
| schemas | `components.schemas` |
| engineering metadata | `x-devxstream-engineering` |

The projection intentionally does not claim lossless conversion: DADS engineering metadata remains a DevXStream extension, and consumers that do not understand it may safely ignore it.
