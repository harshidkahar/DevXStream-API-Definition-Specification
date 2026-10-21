# DADS Authoring Guide

## Start with a minimal definition

```json
{
  "$schema": "https://devxstream.com/schemas/dads/v1.json",
  "definitionVersion": "DADS 1.0",
  "application": { "name": "Inventory API", "version": "1.0.0" },
  "modules": [
    {
      "name": "Inventory",
      "endpoints": [
        {
          "name": "GetStock",
          "method": "GET",
          "route": "/stock/{sku}",
          "summary": "Get stock",
          "parameters": [{ "name": "sku", "in": "path", "type": "string", "required": true }],
          "responses": [{ "statusCode": "200", "description": "Stock level" }]
        }
      ]
    }
  ]
}
```

## Authoring rules

- `definitionVersion`, `application`, and at least one `module` are required.
- Each operation requires a unique HTTP method/route pair, a stable `name`, a `summary`, and one or more responses.
- Every `{routeParameter}` must have a matching required `path` parameter.
- Reusable schemas are named under `schemas`; request and response `schema` values must reference one of these names.
- Use examples that contain no credentials, personal data, or production identifiers.
- Put consumer-facing behaviour in the contract fields. Put CQRS, handlers, caching, authorization, and transaction guidance in `engineering`.

## Validate and build

```powershell
dotnet run --project src/DevXStream.DADS.Cli -- validate inventory.dads.json
dotnet run --project src/DevXStream.DADS.Cli -- export-openapi inventory.dads.json --output openapi.json
dotnet run --project src/DevXStream.DADS.Cli -- build-docs-data inventory.dads.json --output docs-data.json
```

Use the ECommerce definition in `samples/ecommerce.dads.json` as a richer reference.
