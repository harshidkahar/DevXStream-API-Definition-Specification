# DADS v1 Scope and Glossary

## Purpose

DADS defines a portable REST API contract plus optional engineering metadata used by DevXStream tooling. It is the source definition for validation, generated documentation, code-generation inputs, and OpenAPI projections.

## In scope for DADS v1

- Application identity, version, description, framework, and architecture label.
- Servers/environments.
- Modules, tags, and ordered REST operations.
- HTTP method, route, summary, description, operation ID, deprecation, and examples.
- Path, query, header, and cookie parameters.
- Request bodies, responses, media types, reusable schemas, and validation constraints.
- Security requirements and documented error responses.
- Optional DevXStream engineering metadata: CQRS intent, handlers, authorization, validation, cache, transaction, and persistence hints.
- Namespaced extensions.

## Explicitly out of scope for v1

- Runtime hosting/deployment configuration.
- UI/workflow definitions, visual editing, and collaboration.
- Event-driven, GraphQL, gRPC, SOAP, or database-schema modelling as first-class contracts.
- SDK generation and live request execution.
- A claim of lossless conversion from every OpenAPI document.

## Contract versus engineering metadata

The **contract** describes what an API consumer observes: routes, inputs, outputs, errors, and security requirements. **Engineering metadata** describes how DevXStream may implement the contract. Consumers that do not understand engineering metadata must still be able to validate and use the contract.

## Glossary

| Term | Meaning |
| --- | --- |
| Definition | One DADS document describing an application API. |
| Module | A bounded group of related operations, analogous to an OpenAPI tag. |
| Operation | A single HTTP method and route combination. |
| Schema | A reusable data contract identified by name. |
| Request body | A payload supplied to an operation. |
| Response | A documented outcome keyed by HTTP status code. |
| Example | A representative, non-sensitive value for an input or output. |
| Engineering metadata | Optional DevXStream implementation hints. |
| Extension | A namespaced property understood by a specialised tool. |
| Projection | A derived representation, such as OpenAPI 3.1 or the docs site model. |
