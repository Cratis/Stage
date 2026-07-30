---
title: URLs of a running Stage
description: Every URL a play session exposes — the model's command and query endpoints, the Scalar reference, the introspection endpoints, and the Chronicle Workbench.
---

A play session serves two things on two ports: **the model's own API** on `9090`, and **the Chronicle Workbench**
on `35000`. Everything below assumes both were published as themselves:

```bash
docker run --rm -p 9090:9090 -p 35000:35000 -v "$PWD":/eventmodel cratis/stage:latest
```

Nothing needs to be discovered by hand — start at the Scalar reference and click.

## Start here

| URL | What it is |
|---|---|
| `http://localhost:9090/scalar/v1` | **The API reference.** A Scalar UI over the model's operations, with a request builder that executes them. `http://localhost:9090/scalar` redirects here. |
| `http://localhost:9090/openapi/v1.json` | The OpenAPI document behind it. Framework infrastructure operations are stripped, so it describes **only** the model's own commands and queries. |
| `https://localhost:35000` | **The Chronicle Workbench** — event stores, event types, observers and read models for the session. |

The Scalar page is the authoritative list of what a session exposes: it is generated from the endpoints Arc
actually mapped for the model that was loaded.

## Command endpoints

Every command in the model becomes a `POST`, at a route built from where the command sits in the model —
module, feature, any sub-feature, then the command name, each kebab-cased:

```text
POST /api/<module>/<feature>[/<sub-feature>]/<command>
POST /api/<module>/<feature>[/<sub-feature>]/<command>/validate
```

For an `Invoicing` module with an `InvoiceManagement` feature holding a `RegisterInvoice` command, and an
`Adjustments` sub-feature holding `ApplyDiscount`:

```bash
curl -X POST http://localhost:9090/api/invoicing/invoice-management/register-invoice \
    -H "Content-Type: application/json" \
    -d '{"invoiceNumber":"INV-1042","customerId":"11111111-1111-1111-1111-111111111111"}'

curl -X POST http://localhost:9090/api/invoicing/invoice-management/adjustments/apply-discount \
    -H "Content-Type: application/json" \
    -d '{"invoiceId":"…","percentage":10,"reason":"Loyal customer","requiresReview":false}'
```

The **`/validate`** variant next to every command takes the same body and runs the command through the pipeline
without executing it — that is what a client's form validation calls.

The body is the command's modeled payload, and the reply is Arc's standard `CommandResult` envelope:

```json
{
  "response": { "invoiceNumber": "INV-1042", "customerId": "11111111-1111-1111-1111-111111111111" },
  "correlationId": "a870543c-f442-4ee9-913e-25cecb7b9c7a",
  "isSuccess": true,
  "isAuthorized": true,
  "isValid": true,
  "hasExceptions": false,
  "validationResults": [],
  "exceptionMessages": []
}
```

## Query endpoints

Every read model in the model gets two queries by convention — one for a single instance and one for all of them
— named `Get<ReadModel>ById` and `All<ReadModels>`, kebab-cased into the route:

```text
GET /api/<module>/<feature>/get-<read-model>-by-id?id=<guid>
GET /api/<module>/<feature>/all-<read-models>
```

```bash
curl "http://localhost:9090/api/invoicing/invoice-management/all-invoice-list-read-models"
curl "http://localhost:9090/api/invoicing/invoice-management/get-invoice-list-read-model-by-id?id=$ID"
```

Each answers with Arc's `QueryResult` envelope — `data` plus paging and status:

```json
{
  "paging": { "page": 0, "size": 0, "totalItems": 0, "totalPages": 0 },
  "correlationId": "5134b279-41f5-44b1-b058-7f73e87f6867",
  "data": [],
  "isSuccess": true,
  "isReady": true,
  "isAuthorized": true,
  "isValid": true
}
```

Every query endpoint also accepts the HTTP `QUERY` method, carrying its arguments in a JSON body instead of the
query string — useful when arguments are too large or too structured for a URL.

## Discovering the surface programmatically

Two endpoints list what the session exposes, for tooling that would rather not parse OpenAPI:

| URL | What it returns |
|---|---|
| `http://localhost:9090/.cratis/commands` | Every command: name, namespace, type, and its payload schema. |
| `http://localhost:9090/.cratis/queries` | Every query: name, fully qualified name, read model type, and its arguments schema. |

:::caution
Take the **route** from the OpenAPI document, not from the `route` field these two return. The introspection
route currently carries one extra leading segment (`/api/stage/invoicing/…`) that the mapped endpoint does not
have, so calling it verbatim gives a 404. Names, types and schemas are accurate.
:::

## Framework endpoints

Arc's own infrastructure is served under `/.cratis` on the same port. It is deliberately absent from the OpenAPI
document, but it is there:

| URL | Purpose |
|---|---|
| `/.cratis/me` | The current identity. Unauthenticated in a sandbox session, so it answers `401`. |
| `/.cratis/users`, `/.cratis/tenants` | Development identity endpoints — the users and tenants a client can switch between. |
| `/.cratis/identity-details/schema` | The schema of the identity details the app provides. |
| `/.cratis/queries/ws` | WebSocket transport for observable queries (a plain `GET` fails — it needs the upgrade). |
| `/.cratis/queries/sse`, `/.cratis/queries/sse/subscribe`, `/.cratis/queries/sse/unsubscribe` | Server-Sent Events transport for observable queries. |
| `/.cratis/queries/health` | Observable query health. Answers `202` until it has produced its first result. |

Two request headers are honored on every call: **`X-Correlation-Id`** to correlate a call with what it caused, and
**`X-Tenant-Id`** to pick the tenant.

## The Chronicle Workbench

The session's event store is a real Chronicle event store, and the Workbench is the window into it —
event types, event streams, observers, projections and read models:

```text
https://localhost:35000
```

- **HTTPS, with a development certificate.** The kernel generates a self-signed certificate when none is
  configured, so accept the browser warning once (and use `curl -k` from the command line).
- **Sign in with the base image's development credentials** — user `admin`, password `ChangeMeNow!`.
- **Find the session by its generated name.** Each session gets a Docker-style event store name
  (`brave-mendel`, `nifty-turing`, …), printed in the host's startup log and shown in the Workbench.

The same port also serves the Chronicle API and its OAuth authority, which is what the Workbench itself calls —
`https://localhost:35000/.well-known/openid-configuration` describes it, and `POST /connect/token` issues a token
for API calls:

```bash
curl -k -X POST https://localhost:35000/connect/token \
    -d "grant_type=password&username=admin&password=ChangeMeNow!&scope=openid"
```

:::note
`35000` carries gRPC (HTTP/2) as well — that is how the Stage host talks to the kernel inside the container. You
do not need to publish it for the model's API to work; publish it to get the Workbench.
:::

## What the endpoints do today

The surface above is real and complete — every command and query in the model is mapped, described and callable.
What happens behind them is still being filled in:

- A **command** accepts its payload and echoes it back as the response. It does not yet append the events the
  model says it produces, and the modeled validation rules and authorization policies are not yet enforced on
  the HTTP surface — so a payload that violates a rule still answers `isSuccess: true`. Those rules *are*
  enforced by [the specification runner](../docker/spec-runner.md), which checks them against the model.
- A **query** answers with a well-formed envelope, but with no rows (`data: []`) or no instance — the model's
  projections are registered with Chronicle, and reading their projected documents back is a follow-up.

Use a session to explore the shape a model produces — its routes, payloads and schemas. For verifying behavior,
reach for the specification runner.
