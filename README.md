# DevXStream API Definition Specification (DADS)

DADS is a versioned, language-neutral definition format for REST API contracts and the engineering metadata needed to generate, validate, and document production applications.

The DADS packages support .NET 8 and .NET 9. ASP.NET Core applications should use the matching ASP.NET Core shared framework.

## Sprint 0 baseline

- [v1 scope and glossary](docs/v1-scope-and-glossary.md)
- [Versioning and extension policy](docs/versioning-and-extensions.md)
- [Canonical model and mapping](docs/canonical-model.md)
- [Documentation portal design baseline](docs/docs-portal-design.md)
- [DADS v1 draft JSON Schema](specification/v1/dads.schema.json)
- [Repository standards](CONTRIBUTING.md)

The ECommerce reference definition is in [samples/ecommerce.dads.json](samples/ecommerce.dads.json).

## Validate a definition

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- validate samples/ecommerce.dads.json
```

Run the Sprint 1 validation fixtures with:

```powershell
dotnet run --project tests/DevXStream.DADS.Validation.Tests
```

## Generate projections

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- export-openapi samples/ecommerce.dads.json --output ecommerce.openapi.json
dotnet run --project src/DevXStream.DADS.Cli -- build-docs-data samples/ecommerce.dads.json --output ecommerce.docs.json
```

The OpenAPI exporter creates an OpenAPI 3.1.1 projection. DADS-only engineering metadata is preserved as the `x-devxstream-engineering` extension.

## ASP.NET Core integration

Reference only `DevXStream.DADS.AspNetCore` from a Minimal API project. It includes the DADS model, serialization, validation, and OpenAPI packages as transitive dependencies, so it supports the complete DADS workflow from loading and validation through endpoint mapping.

For the complete runtime experience—validated definition loading, automatic handler discovery, endpoint mapping, and the embedded DevXStream documentation portal—use one registration and one route call:

```csharp
builder.Services.AddDevXStreamDads(options =>
    options.DefinitionPath = Path.Combine(builder.Environment.ContentRootPath, "Definitions", "api.dads.json"));

var app = builder.Build();
app.MapDevXStreamDads(); // maps operations and serves the DevXStream portal at /dads
```

Implement `IDadsOperationHandler` classes whose names match each operation's `engineering.handler` value. The embedded portal provides navigation, search, API/model reference, theme switching, DADS download, OpenAPI download, and engineering details from the validated definition at `/dads/definition.json`; neither Swagger nor Scalar is required.

Existing Minimal API and API Controller projects do not need DADS handler classes. Map their existing routes first, then select `ExistingApplicationEndpoints`; DADS verifies that every defined method and route exists and serves the same native UI:

```csharp
builder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionPath = "Definitions/api.dads.json";
    options.EndpointMode = DadsEndpointMode.ExistingApplicationEndpoints;
});

var app = builder.Build();
app.MapControllers(); // or app.MapGet(...), app.MapPost(...), and so on
app.MapDevXStreamDads();
```

```csharp
builder.Services.AddDadsDefinition(Path.Combine(builder.Environment.ContentRootPath, "Definitions", "ecommerce.dads.json"));

var app = builder.Build();
app.MapDadsEndpoints(handlers => handlers
    .Map("GetProduct", GetProductAsync)
    .Map("CreateOrder", CreateOrderAsync));
app.MapDadsOpenApi(); // serves /openapi.json for Scalar, Swagger UI, or any OpenAPI client
```

Every declared operation must have exactly one handler. Each mapped endpoint preserves its DADS operation name and adds `DadsOperationMetadata` to ASP.NET Core endpoint metadata, making it available to conventions, filters, and documentation tooling. Use the existing resolver overload only when a handler needs access to the complete `DadsOperationContext`.

For applications with many operations, declare the handler class name in each operation's `engineering.handler` field and let the package discover handlers automatically:

```csharp
builder.Services
    .AddDadsDefinition(Path.Combine(builder.Environment.ContentRootPath, "Definitions", "api.dads.json"))
    .AddDadsOperationHandlersFromAssemblyOf<GetProductQueryHandler>();

var app = builder.Build();
app.MapDadsEndpoints();

public sealed class GetProductQueryHandler : IDadsOperationHandler
{
    public async Task<IResult> HandleAsync(HttpContext context, DadsOperationContext operation) =>
        Results.Ok(await catalog.GetAsync(context.Request.RouteValues["id"]));
}
```

The convention matches `GetProductQueryHandler` to `"handler": "GetProductQueryHandler"`. Use `[DadsOperationHandler("OtherHandlerName")]` only when a class name cannot match the DADS metadata.

## Generate ASP.NET Core endpoints

For a DADS-first API, generate the endpoint contracts from the definition instead of maintaining route mappings by hand:

```powershell
dotnet tool install --global DevXStream.DADS.Cli --version 1.0.1
dads generate-aspnet Definitions/api.dads.json --output Generated/DadsEndpoints.g.cs
```

Include the generated `.g.cs` file in the API project. It contains `MapGeneratedDadsEndpoints()` and one handler interface per DADS operation, named from `engineering.handler`. Register all implementations from one assembly and map every generated route in one call:

```csharp
builder.Services.AddDadsGeneratedOperationHandlersFromAssemblyOf<GetProductQueryHandler>();

var app = builder.Build();
app.MapGeneratedDadsEndpoints();
```

Regenerate the file whenever the definition changes; no per-operation route map needs to be maintained in application startup.

## Build the branded portal

```powershell
npm install --prefix apps/dads-docs-web
npm run build:dads --prefix apps/dads-docs-web
```

The command projects `samples/ecommerce.dads.json`, copies the generated documentation/OpenAPI data into the Angular portal, and produces the static site at `apps/dads-docs-web/dist/dads-docs-web/browser`.

See the [authoring guide](docs/authoring-guide.md) for the DADS format and [CI/CD guide](docs/ci-cd.md) for validation and static-site build examples.

## Developer preview quality gates

Run the preview verification suite after building the portal:

```powershell
dotnet run --project tests/DevXStream.DADS.Validation.Tests --no-build
npm run verify:preview --prefix apps/dads-docs-web
npm run verify:a11y --prefix apps/dads-docs-web
```

See the [security and processing limits](docs/security-and-limits.md) and [developer preview guide](docs/developer-preview.md) for rollout, feedback, and rollback guidance.

Live request execution is opt-in and sandbox-only. See the [try-it policy](docs/try-it-policy.md) before enabling it, and the [OpenAPI import assessment](docs/openapi-import-assessment.md) for compatibility scope.

## DADS v1.0 release

- [v1.0 specification](docs/specification-v1.md)
- [Draft-to-v1 migration notes](docs/migration-to-v1.md)
- [FAQ and support path](docs/faq.md)
- [Changelog](CHANGELOG.md)
- [Release checklist](docs/release-checklist.md) and [quality gate](docs/release-quality-gate.md)

The library packages use the MIT license; see [LICENSE](LICENSE). The ECommerce sample remains the complete installation, validation, portal-generation, and OpenAPI-export reference.
