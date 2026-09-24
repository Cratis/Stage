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
module, feature, any sub-feature, **slice**, then the command name, each normalized using Arc's route conventions.
The canonical route always includes the command name, even when it is the only command:

```text
POST /api/<module>/<feature>[/<sub-feature>]/<slice>/<command>
POST /api/<module>/<feature>[/<sub-feature>]/<slice>/<command>/validate
```

For an `Invoicing` module with an `InvoiceManagement` feature holding a `RegisterInvoice` slice and command,
and an `Adjustments` sub-feature holding an `ApplyDiscount` slice and command:

```bash
curl -X POST http://localhost:9090/api/invoicing/invoice-management/register-invoice/register-invoice \
    -H "Content-Type: application/json" \
    -d '{"invoiceNumber":"INV-1042","customerId":"11111111-1111-1111-1111-111111111111"}'

curl -X POST http://localhost:9090/api/invoicing/invoice-management/adjustments/apply-discount/apply-discount \
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
GET /api/<module>/<feature>[/<sub-feature>]/<slice>/get-<read-model>-by-id?id=<id>
GET /api/<module>/<feature>[/<sub-feature>]/<slice>/all-<read-models>
```

For the `InvoiceList` slice and its `InvoiceListReadModel`:

```bash
curl "http://localhost:9090/api/invoicing/invoice-management/invoice-list/all-invoice-list-read-models"
curl "http://localhost:9090/api/invoicing/invoice-management/invoice-list/get-invoice-list-read-model-by-id?id=$ID"
```

Both queries read the documents the modeled projection built in the session's Chronicle, using the first 500
instances of the read model. `All<ReadModels>` returns that window. `Get<ReadModel>ById` searches the same window
for an instance whose identity matches `id` (compared case-insensitively) and returns `data: null` when none does,
including an instance that exists beyond the first 500.

Each answers with Arc's `QueryResult` envelope. On success, `isAuthorized` is `true` and `data` holds the
instances — an array for `All<ReadModels>`, a single object for `Get<ReadModel>ById`. Each instance carries its
identity as `id` next to the properties the projection wrote:

```json
{
    "data": [
        {
            "id": "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1",
            "name": "Test item"
        }
    ],
    "isAuthorized": true
}
```

**Pass the `id` a previous response returned, URL-encoded — do not assume it is a GUID.** Stage reads the identity
from the kernel document and normalizes it to text:

| Kernel identity                   | Returned `id`                                                                                          |
| --------------------------------- | ------------------------------------------------------------------------------------------------------ |
| String                            | The string, unchanged (it can be empty)                                                                |
| Number                            | Invariant text, so `42.0` is `42`                                                                      |
| Boolean                           | `True` or `False`                                                                                      |
| Composite object of scalar values | The values joined with `_`, ordered by property name, so `{"region":"west","number":42}` is `42_west` |

When a document has a lowercase `id`, only that field binds the identity; a modeled property named `Id` or `ID` stays
an ordinary property. A document without a lowercase `id` falls back to a legacy uppercase `Id`.

A document Stage cannot read fails the **whole** query with `InvalidStageReadModelDocument` rather than being
silently dropped from the result. That covers a document that is not a JSON object or is not valid JSON, a missing
or `null` identity, and an identity that is an array or a composite containing a `null` or array value.
Arc returns it as an error result (`hasExceptions`, HTTP 500).

**Modeled query authorization is not enforced yet.** Stage does not yet receive Screenplay's executable query authorization, so a modeled `authorize` on a query is not
evaluated: a session's queries allow anonymous access and answer anyone who can reach the port. The session is a
disposable sandbox holding only the events it appended, but treat it as unprotected — bind its published ports to
loopback or reach it only through an authenticated proxy. Filters in Arc's query pipeline can still reject a
query; a rejected query answers `403` with `isAuthorized: false` and no data, without reading Chronicle.

Every query endpoint also accepts the HTTP `QUERY` method, carrying its arguments in a JSON body instead of the
query string — useful when arguments are too large or too structured for a URL. The alternate method has the same
outcomes as `GET`: the same data, the same errors, and the same pipeline rejections. Set
`Cratis:Arc:GeneratedApis:EnableQueryHttpMethod` to `false` to disable `QUERY` on both canonical paths and
compatibility aliases without disabling `GET`. Canonical query names are always included.
OpenAPI describes the `GET` operation; Arc keeps the alternate `QUERY` transport out of API description while
mapping it at the same canonical path when enabled.

## Legacy compatibility and admission

Legacy aliases preserve **actual historical routes**, not guessed command-suffix routes:

- A feature/sub-feature with **one command** historically mapped `POST /api/<module>/<feature>[/<sub-feature>]`
  and its `/validate` variant. For the singleton `RegisterInvoice` example, the aliases are
  `/api/invoicing/invoice-management` and `/api/invoicing/invoice-management/validate`.
- With multiple commands at the old feature location, Arc included each command name. Unique old command-name
  routes and their validation routes remain aliases.
- Conventional query aliases omit the slice, for both `GET` and `QUERY`, when uniquely owned.

An alias is registered only if its **HTTP method and normalized path** uniquely identify an operation across the
complete generated Stage command/query surface. Aliases execute the same Arc handler with the same metadata
semantics, not a redirect. They are excluded from OpenAPI and introspection advertises only canonical routes.

For example, `Orders / Checkout / PlaceOrder / DoIt` and `Orders / Checkout / CancelOrder / DoIt` remain distinct
commands and have stable canonical routes:

```text
POST /api/orders/checkout/place-order/do-it
POST /api/orders/checkout/cancel-order/do-it
```

The ambiguous legacy `POST /api/orders/checkout/do-it` and `/api/orders/checkout/do-it/validate` are **not registered**;
both return **404**, never the first or last declared command. Changing declaration order does not change ownership.

A canonical collision is different: Stage refuses to start with **`AmbiguousStageHttpSurface`**. Diagnostics identify
the HTTP method, normalized path, operation kinds, slice IDs, and qualified artifacts. This admission also rejects
case/kebab/sanitization collisions, omitted collection/type identity collisions, cross-depth execute/validation
collisions, and a canonical route that would take over another operation's legacy URL—even an ambiguous legacy URL.
The entire modeled surface is admitted before providers, dynamic CLR types, modeled endpoint mapping, or Chronicle
connection; a rejected model does not publish a partial modeled surface. This is an HTTP-host boundary, not a
compiler restriction on equal command names in different slices. No collection segments, hashes, or order-based
suffixes are invented to resolve conflicts.

## Discovering the surface programmatically

Two endpoints list what the session exposes, for tooling that would rather not parse OpenAPI:

| URL                                      | What it returns                                                                     |
| ---------------------------------------- | ----------------------------------------------------------------------------------- |
| `http://localhost:9090/.cratis/commands` | Every command: name, namespace, type, and its payload schema.                       |
| `http://localhost:9090/.cratis/queries`  | Every query: name, fully qualified name, read model type, and its arguments schema. |

The `route` fields use the same host-owned generated-API options as endpoint mapping: canonical paths include
the slice and do **not** include an extra `/stage` segment. Command CLR names and fully qualified query identities
are unchanged. Compatibility aliases do not add extra introspection entries.

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
- A **query** reads the first 500 projected documents of its read model from Chronicle and returns them, or the
  one matching `id`. Modeled query authorization is not yet enforced; only Arc query-pipeline filters can reject
  a query. A document Stage cannot read fails the whole query with `InvalidStageReadModelDocument`.
- The **specification runner** checks modeled facts and expectations, but that verification is model-level. It is
  not a substitute for executing every slice through the runtime or a rendered application.
- The separate **legacy syntax-based renderer** writes reviewable backend source. It preserves role-only alternatives and
  authenticated-only authorization exactly on each generated query method. Unsupported authorization raises
  `STAGE-AUTH-001` and faults the render operation. Because output is currently written directly, a failed target
  is unsafe and incomplete: stale files, including a prior copy of a blocked artifact, can remain physically
  present. The advisory `.stage-render-failed` marker does not disable or delete them. Frontend/UI rendering and
  other model constructs remain incomplete.

Use a session to explore routes, payloads, appended facts, and schemas. Use the specification runner for modeled
expectations, and review/build the renderer output when evaluating generated application behavior.
