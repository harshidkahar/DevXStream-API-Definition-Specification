# DADS CI/CD Integration

## GitHub Actions

The repository workflow validates the DADS sample, runs the validation/projection suite, and builds the static portal. To use the same pattern in another repository:

```yaml
- run: dotnet run --project src/DevXStream.DADS.Cli -- validate api.dads.json
- run: dotnet run --project src/DevXStream.DADS.Cli -- export-openapi api.dads.json --output artifacts/openapi.json
- run: npm ci --prefix apps/dads-docs-web
- run: npm run build:dads --prefix apps/dads-docs-web
```

Publish `apps/dads-docs-web/dist/dads-docs-web/browser` as a static-site artifact.

## Azure DevOps

```yaml
- script: dotnet run --project src/DevXStream.DADS.Cli -- validate api.dads.json
- script: npm ci --prefix apps/dads-docs-web
- script: npm run build:dads --prefix apps/dads-docs-web
```

Set the publish artifact path to `apps/dads-docs-web/dist/dads-docs-web/browser`.

## Runtime branding

The generated site reads `data/branding.json` at load time. Change its title, primary colours, or logo path and deploy the changed configuration/assets; no Angular rebuild is needed. The DevXStream default uses the supplied logo unchanged.
