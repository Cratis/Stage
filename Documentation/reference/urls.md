---
title: URLs of a running Stage
description: Every URL a play session exposes — the model's command and query endpoints, the Scalar reference, the introspection endpoints, and the Chronicle Workbench.
---

A play session serves everything a caller needs on **one port**: the model's own API, the Scalar reference and
the Chronicle Workbench, all on `9090`. Everything below assumes it was published as itself:

```bash
docker run --rm -p 9090:9090 -v "$PWD":/eventmodel cratis/stage:latest
```

Nothing needs to be discovered by hand — start at the Scalar reference and click.

## Start here

| URL                                     | What it is                                                                                                                                                |
| --------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `http://localhost:9090/scalar/v1`       | **The API reference.** A Scalar UI over the model's operations, with a request builder that executes them. `http://localhost:9090/scalar` redirects here. |
| `http://localhost:9090/openapi/v1.json` | The OpenAPI document behind it. Framework infrastructure operations are stripped, so it describes **only** the model's own commands and queries.          |
| `http://localhost:9090/workbench`       | **The Chronicle Workbench** — event stores, event types, observers and read models for the session.                                                       |

The Scalar page is the authoritative list of what a session exposes: it is generated from the endpoints Arc
actually mapped for the model that was loaded. Reached through a path-prefixed reverse proxy that sends the
`X-Forwarded-*` headers, both it and the OpenAPI document's `servers` entry report the public address — see
[Behind a reverse proxy](../docker/index.md#behind-a-reverse-proxy).

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
    "response": {
        "invoiceNumber": "INV-1042",
        "customerId": "11111111-1111-1111-1111-111111111111"
    },
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

Each answers with Arc's `QueryResult` envelope. Modeled query performers currently deny authorization and expose
no data because Stage does not yet receive Screenplay's executable query authorization semantics. The relevant
fields therefore report a fail-closed result:

```json
{
    "data": null,
    "isAuthorized": false
}
```

Every query endpoint also accepts the HTTP `QUERY` method, carrying its arguments in a JSON body instead of the
query string — useful when arguments are too large or too structured for a URL. The alternate method has the same
fail-closed behavior.

## Discovering the surface programmatically

Two endpoints list what the session exposes, for tooling that would rather not parse OpenAPI:

| URL                                      | What it returns                                                                     |
| ---------------------------------------- | ----------------------------------------------------------------------------------- |
| `http://localhost:9090/.cratis/commands` | Every command: name, namespace, type, and its payload schema.                       |
| `http://localhost:9090/.cratis/queries`  | Every query: name, fully qualified name, read model type, and its arguments schema. |

:::caution
Take the **route** from the OpenAPI document, not from the `route` field these two return. The introspection
route currently carries one extra leading segment (`/api/stage/invoicing/…`) that the mapped endpoint does not
have, so calling it verbatim gives a 404. Names, types and schemas are accurate.
:::

## Framework endpoints

Arc's own infrastructure is served under `/.cratis` on the same port. It is deliberately absent from the OpenAPI
document, but it is there:

| URL                                                                                          | Purpose                                                                                  |
| -------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `/.cratis/me`                                                                                | The current identity. Unauthenticated in a sandbox session, so it answers `401`.         |
| `/.cratis/users`, `/.cratis/tenants`                                                         | Development identity endpoints — the users and tenants a client can switch between.      |
| `/.cratis/identity-details/schema`                                                           | The schema of the identity details the app provides.                                     |
| `/.cratis/queries/ws`                                                                        | WebSocket transport for observable queries (a plain `GET` fails — it needs the upgrade). |
| `/.cratis/queries/sse`, `/.cratis/queries/sse/subscribe`, `/.cratis/queries/sse/unsubscribe` | Server-Sent Events transport for observable queries.                                     |
| `/.cratis/queries/health`                                                                    | Observable query health. Answers `202` until it has produced its first result.           |

Two request headers are honored on every call: **`X-Correlation-Id`** to correlate a call with what it caused, and
**`X-Tenant-Id`** to pick the tenant.

## The Chronicle Workbench

The session's event store is a real Chronicle event store, and the Workbench is the window into it —
event types, event streams, observers, projections and read models:

```text
http://localhost:9090/workbench
```

It is served on the **model's own port**, alongside everything else, so publishing `9090` is all a session needs.
Nothing has to be signed into: a play session's kernel runs with authentication turned off, because it is
embedded in the container with its only client and is thrown away with it.

**Find the session by its generated name.** Each session gets a Docker-style event store name (`brave-mendel`,
`nifty-turing`, …), printed in the host's startup log and shown in the Workbench.

Reached through a path-prefixed reverse proxy, the Workbench follows the prefix like the rest of the session —
the Stage tells the page where it is being served from, so a session proxied at
`https://studio.example.com/api/play/<session>/` has its Workbench at `…/api/play/<session>/workbench`.

:::note
The kernel's own port, `35000`, still serves the Workbench directly and carries the gRPC the Stage host uses to
talk to it. Publishing it is optional — everything a caller needs is on `9090`.

Reaching `35000` directly is **HTTPS only**: the port multiplexes HTTP/1.1 and HTTP/2 through ALPN, which
requires TLS, so `http://localhost:35000` returns nothing at all (`ERR_EMPTY_RESPONSE` in a browser,
`curl: (52) Empty reply from server`). The certificate is self-signed, so accept the browser warning once (and
use `curl -k` from the command line).
:::

## What the endpoints do today

The surface above is mapped and described, but runtime semantics are intentionally partial:

- A **command** accepts its payload, evaluates its modeled `produces` mappings, appends the resulting facts to
  Chronicle, and echoes the payload as the response. The modeled command validation rules and authorization
  policies are not yet enforced on the runtime HTTP surface, so this sandbox path must not be treated as a
  production security boundary.
- A **query** is mapped but currently fails closed: authorization is denied, its performer is not executed, and
  no data is returned. Reading projected documents waits for the Screenplay-owned executable query and
  authorization model rather than relying on invented Stage semantics.
- The **specification runner** checks modeled facts and expectations, but that verification is model-level. It is
  not a substitute for executing every slice through the runtime or a rendered application.
- The separate **Cratis renderer** writes reviewable backend source. It preserves role-only alternatives and
  authenticated-only authorization exactly on each generated query method. Unsupported authorization raises
  `STAGE-AUTH-001` and faults the render operation. Because output is currently written directly, a failed target
  is unsafe and incomplete: stale files, including a prior copy of a blocked artifact, can remain physically
  present. The advisory `.stage-render-failed` marker does not disable or delete them. Frontend/UI rendering and
  other model constructs remain incomplete.

Use a session to explore routes, payloads, appended facts, and schemas. Use the specification runner for modeled
expectations, and review/build the renderer output when evaluating generated application behavior.
