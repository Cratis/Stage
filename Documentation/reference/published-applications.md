---
title: Published applications and export
description: How to export a versioned application package from a model and run it in published mode, with persistent storage, sign-in by a trusted proxy, and no tooling surfaces.
---

The default Stage is a play sandbox. An application **published** for real users needs the opposite on every point,
so the host has a second mode and a model has a way to become an immutable, versioned unit that runs in it.

## Export a package

```bash
dotnet Cratis.Stage.Host.dll export \
    --model ./Shop \
    --version 1.2.0 \
    --name my-shop \
    --output ./out/my-shop-1.2.0
```

| Argument                    | Meaning                                                                                                    |
| --------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `--model <path>`            | A `.play` file or a folder of them. Required.                                                              |
| `--version <semver>`        | The application version of this package. Required.                                                         |
| `--application-name <name>` | The application name recorded in the manifest. Defaults to the model folder name, or the file name without its extension. |
| `--name <publishedName>`    | The host label the application is published under, as in `https://my-shop.cratis.app`. Required.           |
| `--output <dir>`            | Where to write the package. Must be absent or empty. Required.                                             |
| `--publication-id <id>`     | The publication this package belongs to. Defaults to a value derived from the name, version and model.     |
| `--notes`                   | The package asks for the Notes backend.                                                                    |
| `--runtime-version <semver>`| The Stage runtime the package requires. Defaults to the runtime that exports.                              |
| `--references <file.json>`  | The identifiers the source model gave to its elements: `{ "module:Shop": "id", "slice:Shop.Orders.PlaceOrder": "id" }`. Keys are `module`, `feature`, `slice` or `screen` followed by the dot-separated qualified name. A key the model does not have is refused. |

The exit code is `0` on success, `1` when the model is refused and `2` on a usage error.

### Export in-process from a library

The `Cratis.Stage` NuGet package exports without a file system or a process: pass the `.play` sources as text and get
the package back as bytes.

```csharp
using Cratis.Stage.Publishing;

var package = StageApplicationExport.Export(new StageApplicationExportRequest(
    new Dictionary<string, string> { ["shop.play"] = source },   // relative path -> .play text
    ApplicationName: "Shop",
    ApplicationVersion: "1.2.0",
    PublishedName: "my-shop")
{
    NotesEnabled = false,                                         // optional
    ModelReferences = references                                   // optional, as for --references
});

PackageManifest manifest = package.Manifest;                      // typed
string manifestJson = package.ManifestJson;                       // as written to manifest.json
IReadOnlyDictionary<string, byte[]> files = package.Files;        // manifest.json, Dockerfile, model/...
byte[] archive = package.ToArchive();                             // deterministic .tar.gz
```

`ExportAsync` is the asynchronous form. A request that fails validation, a model that fails admission and a model with no
sign-in requirement throw `StageExportRefused`, whose `Reasons` lists every problem. `ApplicationName` is explicit, so
the package never depends on a folder name. The command line `export` is this API with a file system reader in front of it.

### The archive

`ToArchive()` returns a gzip-compressed tar with flat paths from the package root (`manifest.json`, `Dockerfile`,
`model/<relative path>`), regular files only, sorted by path, owner uid and gid 0, a fixed modification time and no
names. The same package gives the same bytes. Hand it to Kubernetes as a single key of a ConfigMap or Secret, mounted
as a file, and point `Stage:Published:PackagePath` at it.

Export refuses a model that fails the semantic admission the Stage applies when it loads a model, and a model in which
no command or query requires a signed-in caller, because a published application must require sign-in.

### What is in the package

```text
manifest.json
model/            the model files, verified at export time
Dockerfile        FROM cratis/stage:<runtime version>, with the package baked in
```

The export is reproducible: the same model and arguments give the same bytes, with sorted keys and no timestamps. The
manifest records a digest of `model/`, and a published host refuses a package whose model no longer matches it.

### manifest.json

```json
{
  "schemaVersion": 1,
  "applicationName": "Shop",
  "publishedName": "my-shop",
  "applicationVersion": "1.2.0",
  "stageRuntimeVersion": "5.3.0",
  "publicationId": "pub-a7dbf0aa7afba0f92a6305f9734b17d0",
  "modelDigest": "sha256:0ac3ab98…",
  "notesEnabled": false,
  "authentication": {
    "required": true,
    "policies": ["Members"],
    "roles": ["Member"],
    "claims": [{ "name": "tenant", "value": null }],
    "opaquePolicies": false
  },
  "modelReferences": [
    { "kind": "module", "name": "Shop", "id": "studio-module-1" },
    { "kind": "slice", "name": "Shop.Orders.PlaceOrder", "id": "studio-slice-9" }
  ],
  "sinks": [
    { "name": "Reporting", "kind": "readmodels" }
  ]
}
```

`authentication` is derived from the policies the model applies to its operations, so an operator can check that a
sign-in provider supplying those roles and claims is configured. `opaquePolicies` is true when a policy is a code
body that cannot be inspected. `modelReferences` lists only the elements whose identifier the source model supplied; it
is empty when none were supplied. `sinks` lists the [named sinks](sinks.md) the model declares, each with its kind
(`readmodels` or `graphs`), in declaration order, so a deployment can check that it maps every name; it is empty when
the model declares none, and a manifest written before it existed reads as empty.

### Image path convention

The Dockerfile copies the package to `/stage/package` (`manifest.json` and `model/`) and sets
`Stage__Mode=Published` and `Stage__Published__PackagePath=/stage/package`. Build it from the output folder:

```bash
docker build -t registry.example.invalid/my-shop:1.2.0 ./out/my-shop-1.2.0
```

## Run in published mode

Published mode is selected by `Stage:Mode=Published` (environment `Stage__Mode=Published`) or `--published`. The
host loads the model from the package at startup and never from `/stage/load`. Warm mode is not allowed.

The host **refuses to start**, with a message naming each problem, unless all of these are configured:

| Key                                           | Meaning                                                                                                  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `Stage:Published:PackagePath`                 | The package folder, or a `.tar.gz` / `.tgz` archive of one, which is extracted to a temporary folder at startup and verified exactly like a folder. Entries that leave the package, links and special files are refused. Default `/stage/package`. |
| `Stage:Published:Storage:ConnectionString`    | The connection string of the database the bundled Chronicle kernel stores events and read models in, for the engine `Stage:Published:Storage:Type` names. Or `…ConnectionStringFile`, the path of a mounted secret holding it. Not required with an [external kernel](#external-chronicle-kernel). |
| `Stage:Published:Authentication:Mode`         | `TrustedProxy`, the only supported mode.                                                                 |
| `Stage:Published:TrustedProxies`              | An array of IP addresses or CIDR networks the sign-in proxy connects from, for example `127.0.0.1` and `::1` for a proxy in the same pod. |
| `AllowedHosts`                                | The published host names. `*` and empty are refused.                                                     |

Optional keys:

| Key                                           | Meaning                                                                                                  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `Stage:Published:Storage:Type`                | The database engine of the bundled kernel: `MongoDB` (default), `PostgreSql` or `MsSql`, in any case. Any other value exits with code `78`, naming the accepted ones. See [Storage engine](#storage-engine). |
| `CRATIS_CHRONICLE_CONNECTION_STRING_FILE`     | The path of a mounted Chronicle connection string. When it names a readable, non-empty file, the application uses that kernel instead of the bundled one; see [External Chronicle kernel](#external-chronicle-kernel). |
| `Stage:Published:Chronicle:ClientId`, `…ClientSecret` (or `…ClientSecretFile`) | Credentials the host presents to its Chronicle kernel. Both or neither. |
| `Stage:Published:EventStore`                  | The Chronicle event store name. Defaults to `app-<publishedName>`; see [Event store](#event-store). |
| `Stage:Published:Notes:Enabled`               | Hosts the Notes backend. Defaults to the manifest's `notesEnabled`.                                      |
| `Notes:ApplicationName`, `Notes:PublicationId` | Required when Notes is enabled. The name the receiving side sees on every note, and the publication the application runs as. |
| `Notes:Transport`                             | `SameKernel` (default) or `Http`. With `Http`, also `Notes:CollectorUrl`, `Notes:CollectorTokenFile` (or `Notes:CollectorToken`) and `Notes:CollectorTenantId`. |

The `Notes` section is the `NotesOptions` of the Showrunner Notes backend, so every option it documents can be set from the
environment, for example `Notes__Transport=Http`. The host refuses to start, naming what is missing, when Notes is enabled and
the options are incomplete. The browser gets the feedback button and the slice markers only when Notes is enabled.

The package must also be intact (digest matches), require this runtime version and declare an authentication
requirement. Anything else exits with code `78` and a message on standard error.

#### Event store

Published applications share one storage server, so each application's Chronicle data is isolated by its event store name.
In a play session that name is a random Docker-style one; a published host never uses a random name. It is
`app-<publishedName>` from the package manifest, the same on every restart, redeploy and version, and different for every
application. `Stage:Published:EventStore` overrides it, for example to keep the data of an application that was published
under another name. Notes works in this same event store; its `Notes:CollectingStores` name the stores of the
*receiving* side (the default is `Studio`), never this one, and the application identifies itself to the collector by
`Notes:ApplicationName` and `Notes:PublicationId`, so a collecting store must not be given an `app-` name that matches an application.

#### Notes and Chronicle registration

Stage registers its own artifacts over raw Chronicle contracts, so the Chronicle client's automatic discovery is off. Switching it
on for Notes is not safe: the client's registration is a full set that tells the kernel to retire every client-owned projection it
does not list, which would retire Stage's own. Instead, when Notes is enabled the host registers the Notes event types (private
facts and public contract events), its read model reducers and its outbox publisher reactor in the event store of each namespace,
using the client's discovery scoped to the two Notes assemblies, and never runs the full registration. Notes artifacts of a kind
that cannot be registered that way (projections, constraints) make the host report an error instead of being skipped. When Notes is
not enabled, Arc does not serve its commands and queries.

### Authentication

`Stage:Published:Authentication:Mode` selects `TrustedProxy` or `Oidc`. Missing or invalid authentication configuration
refuses startup with exit code `78`. Sandbox personas are disabled in both modes. AuthProxy remains available.

#### TrustedProxy

The application sits behind a sign-in proxy that the publisher deploys in the same pod, in front of Stage. The proxy
signs users in with the application's identity providers and forwards each request with Arc's Microsoft Identity
Platform principal headers (`x-ms-client-principal`, `x-ms-client-principal-id`, `x-ms-client-principal-name`,
`x-ms-client-principal-idp`). Set the mode to `TrustedProxy` and configure `Stage:Published:TrustedProxies`.

- Arc is told to trust those headers. A request without a principal is unauthenticated, and the model's policies
  (`require authenticated`, roles, claims) are enforced against the real principal.
- A request that does **not** come from a trusted proxy address has the principal headers removed before anything reads
  them, so a caller who reaches the host directly cannot present itself as someone else.
- Sandbox personas are off, and the sandbox's header stripping does not run.
- Forwarded-header trust (`X-Forwarded-Host`, `-Proto`, `-Prefix`) is limited to the same trusted proxies.

#### Oidc

Set `Stage:Published:Authentication:Mode=Oidc` to sign users in inside Stage with Microsoft Entra ID. Configure
`Cratis:Authentication:PublicOrigin` and `Cratis:Authentication:EntraId:TenantId`, `ClientId` and `ClientCredentials`.
The Entra section also accepts `Instance`, `Audience`, `CallbackPath` and `SignedOutCallbackPath`, using the same keys as
[sign-in with Entra ID](../guides/sign-in-with-entra-id.md). Supply credentials through deployment secrets, never in the package.
The public origin must be HTTPS (HTTP is allowed only on loopback), with no credentials, path, query or fragment.
Redirects are pinned to this origin, not the request's Host header.

Interactive navigation challenges Entra; fetches receive `401`. Sessions use secure, HttpOnly, SameSite=Lax cookies.
Service callers use JWT bearer authentication; any Authorization header selects bearer authentication and never falls
back to a session cookie. Cookie requests with unsafe methods and WebSocket upgrades require an Origin matching the
configured public origin. Principal and persona headers are stripped before authentication. Arc policies use the signed-in
principal, not forwarded identity headers. Static files and `/stage/status` also require sign-in. Only GET/HEAD `/healthz`
is exempt from the perimeter; this does not add a health endpoint.

Trusted proxies are optional in this mode. Without them no forwarded headers are trusted; when configured, only those
addresses may forward Host, Proto and Prefix. The deployment must still restrict direct access to Stage.

### Chronicle

The kernel in the container uses the configured database, has authentication enabled, and has its API and Workbench
features off. Data survives a restart. Publish only `9090`. The shared base image declares `35000` for sandbox use,
but in published mode the deployment must keep it internal to the pod: no Service port or host port, and a NetworkPolicy
that prevents other pods reaching it. This release does not bind the kernel to loopback itself; no supported bind
setting has been confirmed from the pinned kernel source. Pod isolation, not a loopback bind, is the boundary.

The host refuses Chronicle connections with `auth=none`, including mounted connection strings. Outside loopback,
`Stage:Published:Chronicle:ClientId` and `ClientSecret` must both be explicitly configured. The kernel's well-known
built-in development client remains a fallback only for loopback connections. Mounted connection strings still win,
but pass the same startup guard; refusal exits with code `78` without printing the connection string.
Published sign-in is validated through `Stage:Published:Authentication:Mode` and the selected mode's configuration,
not the sandbox-only `Stage:Sandbox:ForwardedIdentity` flag.

#### Storage engine

`Stage:Published:Storage:Type` (environment `Stage__Published__Storage__Type`) selects the database the bundled kernel stores in.
The container's entrypoint starts the kernel with `Cratis__Chronicle__Storage__Type` set to the same value, in the kernel's
spelling, and `Cratis__Chronicle__Storage__ConnectionDetails` set to the configured connection string, so the connection string
must be one for that engine:

| Type         | Connection string                                                     | Read models stored in |
| ------------ | --------------------------------------------------------------------- | --------------------- |
| `MongoDB`    | `mongodb://mongo.internal:27017`                                      | MongoDB               |
| `PostgreSql` | `Host=postgres.internal;Database=chronicle;Username=app;Password=…`   | PostgreSQL            |
| `MsSql`      | `Server=sql.internal;Database=chronicle;User Id=app;Password=…`       | SQL Server            |

Leaving the type out keeps `MongoDB`, so a deployment that sets only the connection string runs as before. On a SQL engine
the host registers the read models of the Notes backend with Chronicle's `SQL` sink instead of `MongoDB`, so they land in
the same database as the events; there is no MongoDB to configure. The database names, schemas and connection pooling the
kernel uses for each engine are described in [Chronicle storage configuration](/chronicle/hosting/configuration/storage/).

```bash
Stage__Published__Storage__Type=PostgreSql
Stage__Published__Storage__ConnectionStringFile=/run/secrets/storage
```

#### External Chronicle kernel

A published application can use a Chronicle kernel that already runs in the cluster instead of the one in its container.
Mount the kernel's connection string as a file and point `CRATIS_CHRONICLE_CONNECTION_STRING_FILE` at it. When that names a
readable, non-empty file, the entrypoint starts no kernel, does not require `Stage:Published:Storage:*`, and starts the host
directly; the host connects with the mounted connection. A variable that names a missing, unreadable or empty file exits with
code `78` rather than falling back to the bundled kernel.

```bash
CRATIS_CHRONICLE_CONNECTION_STRING_FILE=/run/secrets/chronicle
Stage__Published__Chronicle__ClientId=my-app
Stage__Published__Chronicle__ClientSecretFile=/run/secrets/chronicle-client-secret
```

The host's guards apply unchanged: a connection with `auth=none` is refused with code `78` (the entrypoint checks this before
starting anything), and outside loopback `Stage:Published:Chronicle:ClientId` and `ClientSecret` must be configured. The
application's data lives in its [event store](#event-store) on that kernel, which is why every application has its own name.
`Stage:Published:Storage:Type` still selects where the Notes read models are registered, so set it to the external kernel's
engine when that kernel stores in PostgreSQL or SQL Server. Neither the entrypoint nor the host prints the connection string.

### Routes

Only the frontend, the model's `/api` operations, Arc's `/.cratis/*` operations, `/stage/status`, `/stage/scene`,
`/stage/routes`, `/stage/locales`, `/stage/strings/{locale}` and `/stage/model-references` are served. Scalar and OpenAPI,
the Workbench proxy, the read-model browser, `/stage/load`, admission, specification runs and the package endpoints are
not mapped. They answer `404`, startup fails if any of them is mapped, and a specification enumerates the mapped
endpoints. See [URLs of a running Stage](urls.md).

### Model references in the frontend

`GET /stage/model-references` returns the application name, published name, version, publication id and the
`modelReferences` of the manifest. `@cratis/stage.runtime` reads it with `fetchModelReferences()` and looks an element
up with `modelReferenceFor(references, 'slice', 'Shop.Orders.PlaceOrder')`.

### Extending the published host

`PublishedHostExtensions.Register(IPublishedHostExtension)` adds a capability. An extension registers services on the
`WebApplicationBuilder` (and so on the Arc application and the Chronicle event store) in `ConfigureServices`, and maps
endpoints in `MapEndpoints`. An extension named `PublishedHostExtensions.Notes` is composed only when
`Stage:Published:Notes:Enabled` (or the manifest) enables Notes, and the host refuses to start when Notes is enabled and no
such extension is registered. Extensions cannot map tooling routes.
