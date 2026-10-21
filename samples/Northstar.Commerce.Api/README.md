# Northstar Commerce API sample

This ASP.NET Core 10 Web API consumes the published DADS NuGet packages and the repository's ECommerce DADS definition.

At startup, it validates `Definitions/ecommerce.dads.json`; the application does not start when the definition is invalid. It also serves:

- `GET /dads/definition` — the source DADS document
- `GET /openapi.json` — OpenAPI 3.1 generated directly from that document
- `GET /products`, `GET /products/{id}`, and `POST /orders` — the documented API operations

## Run

```powershell
dotnet run --project samples/Northstar.Commerce.Api --urls http://localhost:5076
```

Then browse to `http://localhost:5076/openapi.json` or call `http://localhost:5076/products`.

The project uses `DevXStream.DADS.Serialization`, `DevXStream.DADS.Validation`, and `DevXStream.DADS.OpenApi` version `1.0.0` from NuGet.org, demonstrating the public package integration path.
