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
| `--name <publishedName>`    | The host label the application is published under, as in `https://my-shop.cratis.app`. Required.           |
| `--output <dir>`            | Where to write the package. Must be absent or empty. Required.                                             |
| `--publication-id <id>`     | The publication this package belongs to. Defaults to a value derived from the name, version and model.     |
| `--notes`                   | The package asks for the Notes backend.                                                                    |
| `--runtime-version <semver>`| The Stage runtime the package requires. Defaults to the runtime that exports.                              |
| `--references <file.json>`  | The identifiers the source model gave to its elements: `{ "module:Shop": "id", "slice:Shop.Orders.PlaceOrder": "id" }`. Keys are `module`, `feature`, `slice` or `screen` followed by the dot-separated qualified name. A key the model does not have is refused. |

The same operation is available to callers as a library: `Cratis.Stage.Host.Export.StageExport.Run(StageExportRequest)`.
The exit code is `0` on success, `1` when the model is refused and `2` on a usage error.

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
  ]
}
```

`authentication` is derived from the policies the model applies to its operations, so an operator can check that a
sign-in provider supplying those roles and claims is configured. `opaquePolicies` is true when a policy is a code
body that cannot be inspected. `modelReferences` lists only the elements whose identifier the source model supplied; it
is empty when none were supplied.

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
| `Stage:Published:PackagePath`                 | The package folder. Default `/stage/package`.                                                            |
| `Stage:Published:Storage:ConnectionString`    | A MongoDB connection string for Chronicle and its read models. Or `…ConnectionStringFile`, the path of a mounted secret holding it. |
| `Stage:Published:Authentication:Mode`         | `TrustedProxy`, the only supported mode.                                                                 |
| `Stage:Published:TrustedProxies`              | An array of IP addresses or CIDR networks the sign-in proxy connects from, for example `127.0.0.1` and `::1` for a proxy in the same pod. |
| `AllowedHosts`                                | The published host names. `*` and empty are refused.                                                     |

Optional keys:

| Key                                           | Meaning                                                                                                  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `Stage:Published:Chronicle:ClientId`, `…ClientSecret` (or `…ClientSecretFile`) | Credentials the host presents to its Chronicle kernel. Both or neither. |
| `Stage:Published:Notes:Enabled`               | Hosts the Notes backend. Defaults to the manifest's `notesEnabled`.                                      |

The package must also be intact (digest matches), require this runtime version and declare an authentication
requirement. Anything else exits with code `78` and a message on standard error.

### Authentication

The application sits behind a sign-in proxy that the publisher deploys in the same pod, in front of Stage. The proxy
signs users in with the application's identity providers and forwards each request with Arc's Microsoft Identity
Platform principal headers (`x-ms-client-principal`, `x-ms-client-principal-id`, `x-ms-client-principal-name`,
`x-ms-client-principal-idp`). Stage does not run OIDC itself.

- Arc is told to trust those headers. A request without a principal is unauthenticated, and the model's policies
  (`require authenticated`, roles, claims) are enforced against the real principal.
- A request that does **not** come from a trusted proxy address has the principal headers removed before anything reads
  them, so a caller who reaches the host directly cannot present itself as someone else.
- Sandbox personas are off, and the sandbox's header stripping does not run.
- Forwarded-header trust (`X-Forwarded-Host`, `-Proto`, `-Prefix`) is limited to the same trusted proxies.

### Chronicle

The kernel in the container uses the configured MongoDB, has authentication enabled, and has its API and Workbench
features off. Data survives a restart. Do not publish port `35000`; the application image exposes only `9090`.

The host presents the kernel's built-in client unless `Stage:Published:Chronicle:ClientId` and `ClientSecret` are
configured, and this release does not bind the kernel to loopback itself, so the pod's network must keep `35000` internal.

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
