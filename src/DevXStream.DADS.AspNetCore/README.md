# DevXStream.DADS.AspNetCore

`DevXStream.DADS.AspNetCore` is the single-package integration for a DADS-defined ASP.NET Core API. It validates the DADS contract at startup, maps or verifies API endpoints, exports OpenAPI, and hosts the native DADS documentation and API-testing portal.

It brings in the DADS model, serialization, validation, and OpenAPI packages transitively. You do not need to add those packages separately.

## Install

```bash
dotnet add package DevXStream.DADS.AspNetCore
```

## Existing controllers or Minimal APIs

Use this mode when the application already owns its routes. DADS verifies the declared operations against those routes and serves the portal at `/dads`.

```csharp
using DevXStream.DADS.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "Definitions",
        "api.dads.json");
    options.EndpointMode = DadsEndpointMode.ExistingApplicationEndpoints;
});

var app = builder.Build();
app.MapControllers(); // Or map existing Minimal API routes first.
app.MapDevXStreamDads();
app.Run();
```

There is no per-operation handler map and no Swagger or Scalar dependency required.

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

- .NET 10 and ASP.NET Core 10.
- A valid DADS 1.x JSON or YAML definition.

## Links

- [DADS repository](https://github.com/harshidkahar/DevXStream-API-Definition-Specification)
- [DevXStream](https://devxstream.com)
