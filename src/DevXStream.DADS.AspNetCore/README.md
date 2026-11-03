# DevXStream.DADS.AspNetCore

`DevXStream.DADS.AspNetCore` is the single-package integration for DADS in ASP.NET Core. It can discover an existing application at runtime without a contract file, or load a curated DADS JSON or YAML definition when one is supplied. It hosts the native DADS portal and exports OpenAPI from the same validated definition.

It brings in the DADS model, serialization, validation, and OpenAPI packages transitively. You do not need to add those packages separately.

## Install

```bash
dotnet add package DevXStream.DADS.AspNetCore
```

## Existing controllers or Minimal APIs

Use this mode when the application already owns its routes. No `api.dads.json` file is required. Map your existing endpoints before calling `MapDevXStreamDads`; DADS discovers them and serves the portal at `/dads`.

```csharp
using DevXStream.DADS.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDevXStreamDads();

var app = builder.Build();
app.MapControllers(); // Or map existing Minimal API routes first.
app.MapDevXStreamDads();
app.Run();
```

There is no per-operation handler map and no Swagger or Scalar dependency required. Runtime discovery uses the mapped route, HTTP method, endpoint name, tags or controller name, route parameters, and authorization metadata. Optional DADS metadata can be supplied with a custom definition provider when the baseline needs more engineering detail.

## Curated file or custom provider

`DefinitionPath` remains supported for teams that keep a reviewed DADS document. A valid file is preferred in the default `Auto` source mode. If an optional file is missing or invalid, the default runtime fallback keeps the application running and generates the portal from existing endpoints.

```csharp
builder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionPath = Path.Combine(builder.Environment.ContentRootPath, "Definitions", "api.dads.json");
    options.EndpointMode = DadsEndpointMode.ExistingApplicationEndpoints;
    options.EnableRuntimeFallback = true; // default
});
```

Use strict file behavior only when a curated contract is required. In strict mode DADS routes return an availability response if the file is unavailable; the rest of the application, including health endpoints, continues to run.

```csharp
builder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionSource = DadsDefinitionSource.File;
    options.DefinitionPath = "Definitions/api.dads.json";
    options.EnableRuntimeFallback = false;
});
```

For an application-owned definition, register `IDadsDefinitionProvider` and select `CustomProvider` or leave the default `Auto` source mode. Provider output is validated by the same DADS validator used by the portal and OpenAPI endpoint.

## DADS-first endpoints

For a DADS-first application, set `engineering.handler` for each operation and implement matching `IDadsOperationHandler` classes. The default `AutoDiscoverHandlers` mode discovers them from the configured assembly.

```csharp
builder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionPath = "Definitions/api.dads.json";
    options.HandlerAssembly = typeof(Program).Assembly;
});

var app = builder.Build();
app.MapDevXStreamDads();
```

```csharp
public sealed class GetProductHandler : IDadsOperationHandler
{
    public Task<IResult> HandleAsync(HttpContext context, DadsOperationContext operation) =>
        Task.FromResult(Results.Ok());
}
```

## Native DADS portal

`MapDevXStreamDads()` hosts a DevXStream-branded portal at `/dads` and the loaded contract at `/dads/definition.json`. The portal provides:

- searchable, collapsible modules, APIs, and models;
- method, route, request-body, parameter, response, and security documentation;
- schema property type, format, required state, descriptions, and hoverable validation patterns;
- Bearer and API-key authorization stored only for the current browser session;
- an interactive request client with editable URL, headers, cookies, query parameters, request body, multipart file upload, request history, cancellation, and full response inspection;
- selectable cURL, JavaScript, Node.js, C#, Python, and Java code samples, plus copy controls;
- DADS and OpenAPI downloads.

The request client sends a request only when the user selects **Test request** and presses **Send**.

## Security and patterns in a DADS definition

```json
{
  "securitySchemes": {
    "bearerAuth": {
      "type": "http",
      "scheme": "bearer",
      "bearerFormat": "JWT"
    },
    "apiKey": {
      "type": "apiKey",
      "in": "header",
      "name": "X-API-Key"
    }
  },
  "schemas": [{
    "name": "CreateCustomerRequest",
    "type": "object",
    "required": ["email"],
    "properties": {
      "email": {
        "type": "string",
        "format": "email",
        "pattern": "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"
      }
    }
  }]
}
```

Apply a security scheme to an operation with `"security": ["bearerAuth"]`. Property `pattern` is shown in the native portal and exported to the OpenAPI 3.1 document.

## OpenAPI endpoint

The native portal exposes an OpenAPI download. If another tool needs a conventional OpenAPI route, add it explicitly:

```csharp
app.MapDadsOpenApi("/openapi.json");
```

## Requirements

- .NET 8, .NET 9, or .NET 10, with the matching ASP.NET Core shared framework.
- No contract file is required for existing ASP.NET Core applications.
- A valid DADS 1.x JSON or YAML definition is required only when file mode is selected.

## Links

- [DADS repository](https://github.com/harshidkahar/DevXStream-API-Definition-Specification)
- [DevXStream](https://devxstream.com)
