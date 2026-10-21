# Canonical DADS Model and Mapping

## Design rules

The canonical model is independent from JSON, YAML, OpenAPI, and the portal framework. Parsers create it, validators inspect it, and projections consume it. This prevents format-specific behaviour leaking into generation and documentation.

## Proposed .NET public model

```csharp
public sealed record DadsDefinition(
    string DefinitionVersion,
    ApplicationDefinition Application,
    IReadOnlyList<ServerDefinition> Servers,
    IReadOnlyList<ModuleDefinition> Modules,
    IReadOnlyList<SchemaDefinition> Schemas,
    IReadOnlyDictionary<string, JsonElement> Extensions);

public sealed record ModuleDefinition(
    string Name,
    string? Description,
    IReadOnlyList<OperationDefinition> Endpoints,
    IReadOnlyDictionary<string, JsonElement> Extensions);

public sealed record OperationDefinition(
    string Name,
    HttpMethodKind Method,
    string Route,
    string Summary,
    string? Description,
    IReadOnlyList<ParameterDefinition> Parameters,
    RequestBodyDefinition? Request,
    IReadOnlyList<ResponseDefinition> Responses,
    EngineeringMetadata? Engineering,
    IReadOnlyDictionary<string, JsonElement> Extensions);
```

## Source mapping

| DADS JSON | Canonical model | Documentation projection | OpenAPI 3.1 projection |
| --- | --- | --- | --- |
| `application` | `ApplicationDefinition` | Introduction header | `info` |
| `servers` | `ServerDefinition` | Server selector | `servers` |
| `modules[]` | `ModuleDefinition` | Navigation group | `tags` |
| `endpoints[]` | `OperationDefinition` | Endpoint page | `paths` operation |
| `parameters[]` | `ParameterDefinition` | Input contract | `parameters` |
| `request` | `RequestBodyDefinition` | Request panel | `requestBody` |
| `responses[]` | `ResponseDefinition` | Response tabs | `responses` |
| `schemas[]` | `SchemaDefinition` | Models explorer | `components.schemas` |
| `engineering` | `EngineeringMetadata` | Engineering details panel | `x-devxstream-engineering` |

## Validation boundaries

Schema validation verifies document shape. Semantic validation verifies global rules: unique module/operation names, unique method/route pairs, route-parameter matching, schema references, valid response status codes, and known HTTP methods.
