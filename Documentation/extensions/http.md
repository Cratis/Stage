# The built-in `http` extension

Many integrations are one REST call. The built-in `http` extension realizes an ability from a declarative `http` block in the model, with no
extension process and no code. It runs inside Stage, through the same [durable invocation path](../reference/ability-invocations.md) and the
same system gates as any other extension, so retries, idempotency keys, outcomes and reconciliation behave the same.

This page is the reference for the block, the settings it reads and the results it produces. Read
[systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) in the Screenplay documentation for
the `system` and `ability` declarations the block belongs to, and the
[extensions overview](index.md) for how the other extensions connect.

## The block

```screenplay
system Carrier
  extension http version 1
  ability BookPickup idempotent
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
    outcome NoCapacity
    http
      post "/pickups"
      header "Idempotency-Key" = $call.idempotencyKey
      body
        reference = parcelId
        street = address.street
      response 201
        trackingNumber = body.tracking
      response 409 -> NoCapacity
```

The block is structured lines, never JSON. Indentation nests; a line that is not understood is an error that names its line.

| Line | Meaning |
| --- | --- |
| `get`, `post`, `put`, `patch` or `delete` followed by a quoted path | The method and the path, relative to the base address of the system. Exactly one. |
| `header "Name" = value` | Sends a header. |
| `query "name" = value` | Sends a query parameter. |
| `body`, with nested `name = value` lines | Sends a JSON object built from the values. A nested name with indented lines inside it is a nested object. Not allowed on `get` or `delete`. |
| `response <status>` | What a response status means. At least one. Each status once. |
| `response <status> -> Outcome` | The status settles the ability with the named [outcome](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md). |
| `name = body.path` under a response | A value the ability returns, read from the response. |

### Values

| Value | Reads |
| --- | --- |
| `"text"`, `42`, `true` | A literal. |
| `parcelId`, `address.street` | An input of the ability, and a path into it. An optional input that is absent is left out. |
| `$call.idempotencyKey` | The idempotency key of the invocation. Also `$call.attempt`, `$call.tenant`, `$call.system` and `$call.ability`. |
| `body.tracking`, `body.items.count`, `status` | Only under a `response`: a path into the JSON body, or the status code. |

A path can hold `{input}`: `get "/parcels/{parcelId}/events"` replaces it with the input, escaped as one path segment. A path cannot be absolute,
contain `..` or a `?`, and the finished address must stay under the base address of the system. Put parameters in `query` lines.

## Base address and credentials

The model holds no address and no credential. Both come from the settings and secrets of the system, resolved by the
[host settings service](host-services.md) as `systems:<System>:Http:<Name>` (`systems__<System>__Http__<Name>` in the environment of the Stage
process, or any configuration provider such as a secret store):

| Setting | Meaning |
| --- | --- |
| `BaseAddress` | Required. An `http` or `https` address with no user information. |
| `Authentication` | `none` (default), `bearer`, `basic` or `apiKey`. |
| `Token` | The bearer token, for `bearer`. |
| `Username`, `Password` | For `basic`. |
| `ApiKey`, `ApiKeyHeader` | For `apiKey`. The header defaults to `X-API-Key`. |

A block cannot set `Authorization`, `Proxy-Authorization`, `Cookie`, `X-API-Key` or `Host`: that is an error, so a credential cannot slip into the
model. A value is never logged, never put in a failure reason and never sent anywhere but to the system. Logs name the system, the ability and
the status, never an address, a header, a query or a body. Redirects are not followed, so credentials are never forwarded to another host.

## What each response means

| What happened | Result |
| --- | --- |
| A status with a `response` line | The ability returns the mapped values, settled with the outcome of the line when it has one. |
| A mapped value is missing from the body, or the body is not JSON | Failed. The system answered, so the effect may have happened and Stage reconciles it. |
| A `4xx` status without a `response` line, except `408` | Failed with the effect ruled out: the system refused the request. |
| Any other status without a `response` line, a timeout, a dropped or refused connection | Failed with the effect left open, as nothing says whether it happened. |
| A missing or unusable setting, or an input a path needs is absent | Not sent. Nothing left Stage, and the reason names the setting, never its value. |

The timeout is the one of the ability. An ability declared `idempotent` still needs the header the system wants, for example
`header "Idempotency-Key" = $call.idempotencyKey`: Stage supplies the key, the block says where it goes.

## Turn it on

The extension is built into Stage and runs in-process. A host realizes abilities by registering their blocks after the invocations:

```csharp
services.AddStageInvocations(configuration);
services.AddStageHttpAbilities([HttpBlockParser.Parse("Carrier", "BookPickup", block).Definition!]);
```

It decorates the registered ability backend: an ability with an `http` block is realized here, every other ability still goes to the backend that
was registered, such as [a connected extension backend](connect-a-backend.md).

## What is available today

- The built-in is an in-process extension. Packaged extensions, loaded as OCI artifacts, are tracked in
  [Cratis/Stage#1064](https://github.com/Cratis/Stage/issues/1064); the `http` extension moves to that mechanism without changing the block.
- Stage reads the block itself. The Screenplay compiler, Monaco and the VS Code extension validating, highlighting and completing the block through the
  extension's contract is tracked in [Cratis/Screenplay#1253](https://github.com/Cratis/Screenplay/issues/1253), so until then the host
  passes the block text to `HttpBlockParser`.
- Responses are mapped by exact status. Ranges such as `2xx`, response headers and non-JSON bodies are not supported.
- Abilities that answer later (`answers later`) are not supported by this extension.

## See also

- [Extension backends](index.md) and the [host services reference](host-services.md).
- [Durable ability invocations](../reference/ability-invocations.md).
- [Systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) in the Screenplay documentation.
- [Extension invocations](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/extension-invocations.md) in the Choreography documentation: retries, idempotency and reconciliation.
