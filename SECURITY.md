# Security Policy

## Supported versions

Security fixes are provided for the latest `1.0.x` release of the DADS packages and the `main` branch.

## Reporting a vulnerability

Please use the repository's [private security advisory reporting page](https://github.com/harshidkahar/DevXStream-API-Definition-Specification/security/advisories/new) to report a suspected vulnerability.

Include a clear description, affected package and version, proof of concept or reproduction steps, and any suggested mitigation. Do not include credentials, access tokens, or customer data in a report.

We will acknowledge valid reports, investigate them privately, and coordinate a fix before public disclosure.

## Package publishing

NuGet publishing is performed only by the protected `release` GitHub Actions environment through NuGet trusted publishing. Personal NuGet API keys must not be stored in the repository, workflow files, or issue discussions.
