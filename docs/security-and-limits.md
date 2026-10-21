# DADS security and processing limits

DADS treats every definition as untrusted input. The loader limits a source document to 5 MB and JSON nesting to 64 levels. The validator rejects definitions containing more than 250 modules, 1,000 endpoints per module, or 2,000 schemas. These limits keep validation predictable for both local tools and CI.

Invalid JSON/YAML never reaches the projection pipeline. Diagnostic `DADS000` is returned for syntax, size, or depth failures, while structural limits use `DADS106`–`DADS108`.

The generated portal renders all DADS strings using Angular interpolation. It does not use `innerHTML` or HTML trust-bypass APIs, so description and example text is rendered as text rather than executable markup. The portal does not embed credentials or environment-specific secrets; server URLs and downloadable specifications remain definition-owned public data.

For production, retain the limits above at every upload boundary and set the static host's usual response security headers, including `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, and `Referrer-Policy`.
