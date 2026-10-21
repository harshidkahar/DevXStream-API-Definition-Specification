# DADS FAQ

## Is DADS an OpenAPI replacement?

DADS is the source format for DevXStream contract and engineering documentation. It exports OpenAPI 3.1 for interoperability; OpenAPI import is planned as a guided, non-lossless workflow.

## Can I write YAML?

Yes. The loader accepts `.json`, `.yaml`, and `.yml` definitions with equivalent canonical models.

## Does the static portal expose secrets?

It must not. Keep secrets out of DADS servers, examples, branding, and Try-it configuration. The Try-it UI has no credential field and is disabled by default.

## How do I report a problem?

Open an issue in the project tracker with the CLI version, redacted DADS sample, command output, and expected behavior. For a security issue, use the private security contact specified by the hosting organization rather than a public issue.
