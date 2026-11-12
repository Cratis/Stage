# Extension packages: loading, trust and admission

A model names an extension on a [system](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) with
`extension "<reference>"`. Naming a package is not trusting it. Stage resolves the package from the model's lock, decides whether the deployment allows
it, verifies it and only then serves what it ships. A package that fails any step is refused through `/stage/semantic/admission` before anything runs.

This page covers how Stage loads packages. The package format and the lock are specified in the Screenplay documentation:
[extension contracts, the manifest and the lock](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md).
Pinning and restoring by hand is the [`screenplay restore`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#restore-extensions)
command; checking a package before it is published is `cratis extension verify`.

## What Stage loads

Stage reads `screenplay.lock` beside the model root. For each entry it pulls exactly the pinned digest. It never resolves a version range, never follows a
tag and never asks what the newest release is; that is what restore does when a person asks for it, and the lock records the answer.

```text
extension "ghcr.io/cratis/extension-mail"
  range "2"
  resolved "ghcr.io/cratis/extension-mail"
  version 2.1.0
  digest sha256:13c8…
  contract sha256:13c8…
  publisher "https://github.com/cratis"
```

| Lock field | What Stage does with it |
| --- | --- |
| `resolved` | Where to fetch from, and what the allow-list matches a reference against. |
| `digest` | The only thing fetched. Everything downloaded must hash to it. |
| `contract` | The hash of `contract.play`. The package's contract must be exactly this. |
| `publisher` | Whose signing key the signature must verify against, and what the allow-list matches a publisher against. |

A model with no lock, or a lock that pins nothing, loads nothing and needs no configuration.

### Two channels, one package

| Channel | Reference | What it holds |
| --- | --- | --- |
| OCI artifact | A full OCI reference such as `ghcr.io/acme/extension-ledger`, or a catalog short name that resolves to one | A full extension: contract, frontend, Screenplay-authored UI and the backend. The only way to ship a backend. |
| npm | `npm:@acme/charts` | A frontend-only component package. It has no backend, so nothing runs. |

Both give the same thing once loaded: a `contract.play` and files, read without running anything.

**OCI layout.** The artifact is an image manifest whose layers are tar archives, plain or gzipped (media type ending in `tar` or `tar+gzip`, for example
`application/vnd.cratis.extension.layer.v1.tar+gzip`). Everything is under `extension/`:

```text
extension/contract.play
extension/assets/icon.svg
extension/frontend/index.js
extension/ui/*.play
extension/placeholders/*.html
extension/backend/...
```

The manifest is fetched by its digest, each layer is checked against the digest the manifest names, and a manifest index is refused.

**npm layout.** The tarball's `package/` folder is the package, with `contract.play` at its root. The lock pins the sha256 of the tarball.

### What is checked when a package is read

Packages are read as tar archives and never unpacked to disk by the loader. An archive is refused as a whole when it holds a link or device entry, a path that
is absolute or leaves the package, a path that appears twice, a file outside the package folder, more files than `MaxPackageFiles`, or unpacks to more than
`MaxPackageBytes` (counted while it is read, so a compression bomb is stopped). Files under `backend/` are listed but not kept in memory.

## Trust

Stage loads only what the deployment allows. There are two places to say so, and a package must satisfy both when both say something.

### The project: `.cratis/extensions.json`

Stage looks for `.cratis/extensions.json` in the model's folder and the folders above it. This is where a project starts.

```json
{
  "allow": {
    "publishers": ["https://github.com/cratis"],
    "references": ["ghcr.io/cratis/*"]
  },
  "publishers": {
    "https://github.com/cratis": {
      "keys": ["-----BEGIN PUBLIC KEY-----\nMFkw…\n-----END PUBLIC KEY-----"]
    }
  }
}
```

A file that cannot be read allows nothing, and the admission says why.

### The deployment: `Stage:Extensions:Packages`

The deployment narrows what the project allows, and holds the credentials. It binds to [`ExtensionPackageOptions`](#configuration).

```json
{
  "Stage": {
    "Extensions": {
      "Packages": {
        "Allow": { "References": ["ghcr.io/cratis/*"] },
        "Registries": { "ghcr.io": { "Username": "reader", "Password": "<from a secret store>" } }
      }
    }
  }
}
```

### How a package is allowed

- Nothing is allowed until someone lists something. With neither the project nor the deployment listing a publisher or a reference, every package is refused and the
  refusal names where to configure it.
- A list is an exact value, or a prefix ending in `*`.
- Within one source, everything it lists must match: if it lists publishers the publisher must match, and if it lists references the reference must match. An
  allowed publisher can therefore be limited to the references wanted from it.
- When both sources list something, both must allow the package. The deployment can only narrow the project.
- What is matched is the publisher and the resolved reference in the lock, never the name the model uses and never a catalog short name.

### Signatures

A package must carry a signature by a key of the publisher the lock records. The publisher's keys come from `publishers` in the project file or in the
deployment's configuration; keys in the deployment replace the project's for the same publisher, and listing more than one allows a key to be rotated. Keys are
ECDSA P-256 public keys, as `cosign generate-key-pair` writes them, either as PEM or as the base64 body.

| Channel | What is signed and where Stage finds it |
| --- | --- |
| OCI | A [cosign](https://docs.sigstore.dev/cosign/signing/signing_with_self-managed_keys/) key-based signature, `cosign sign --key`, stored as the `sha256-<digest>.sig` artifact next to the package. Stage accepts it only when the signed payload names the locked digest, so a genuine signature of another package or another version never counts. |
| npm | The registry's signature over `<name>@<version>:<integrity>`. Stage computes the integrity from the bytes it downloaded, so the signature only counts for exactly those bytes. |

A signature that does not verify refuses the package even when signatures are not required. Keyless signing (Fulcio and Rekor certificates) is not verified by
this version; a publisher that signs keylessly cannot be trusted by Stage yet.

`Stage:Extensions:Packages:RequireSignatures` defaults to `true`. Only the deployment can turn it off, never the project or a model, and Stage logs a warning at
every start when it is off. It is meant for local development against a registry you run yourself.

## Admission

A package is loaded only when every step passes, in this order. The first failure refuses the package, with its reason.

| Reason | Refused because | What to do |
| --- | --- | --- |
| `NotAllowed` | The deployment and the project do not allow the package or its publisher. Nothing is fetched. | List the publisher or the reference, or remove the extension from the model. |
| `Malformed` | The lock entry has no sha256 digest, contract hash or publisher; no channel serves the reference; the package or its contract is not valid; a file the contract names is missing; a placeholder runs script or loads something external; or a file is not safe to unpack. | Run `cratis extension verify` on the package, or `screenplay restore --update` on the model. |
| `NotFound` | The registry does not have the locked digest, cannot be reached or needs credentials. | Check the lock and the registry credentials in configuration. |
| `DigestMismatch` | What the registry or the cache returned does not hash to the locked digest, or a layer does not hash to the digest in the manifest. | The package was changed after it was locked. Review the change, then restore. |
| `ContractMismatch` | The package's `contract.play` is not the contract the lock pinned. | Review the change, then restore. |
| `NotSigned` | The package carries no signature and signatures are required. | Have the publisher sign it. |
| `SignatureInvalid` | There are signatures, but none by a key of the publisher the lock records covers this package. | The publisher changed, or the signature is for something else. Review, then restore. |
| `TooLarge` | The package is larger than `MaxPackageBytes` or has more than `MaxPackageFiles` files. | Raise the limit if the size is expected. |
| `RequirementsNotMet` | The package requires a Stage or Screenplay version this Stage is not. | Use a release of the package that supports this Stage. |

Refusals are reported with the capability `ExtensionPackage` and the reference as the artifact:

```http
GET /stage/semantic/admission

{
  "engine": "semantic",
  "issues": [ "acme/mail: NotSigned: The package is not signed." ],
  "entries": []
}
```

While any package is refused, the model is not loaded at all, so nothing it names is compiled against or run. In published mode the host refuses to start and
reports the same reasons.

## The cache and pinning

Downloads are kept by digest in `CacheDirectory`, which defaults to `Cratis/Stage/extensions` in the local application data folder. The cache holds bytes and trusts
none of them: every read hashes them against the lock again, and an entry that no longer verifies is downloaded again. A package that was refused is never cached.

The locked digest is part of the render input and is pinned by every workflow run. Every ability of an extension is described with the digest of its locked
package, so upgrading the lock never changes an invocation in flight (see [ability invocations](../reference/ability-invocations.md)). The contracts of loaded
packages are kept where the compiler reads restored contracts, so the model compiles against exactly the contract that was admitted.

## What Stage serves

Everything comes from the files read when the package loaded, which were checked against the locked digest. Addresses contain the digest, so a response never
changes and is served `immutable`, and nothing can be asked for outside the package.

| Route | Serves |
| --- | --- |
| `GET /extension-assets/{id}/{path}` | The frontend: the folder of the contract's `frontend` entry point. Not the contract, not other folders. Served wherever the packages are loaded. |
| `GET /stage/extensions/packages/{id}/icon` | The manifest's icon. |
| `GET /stage/extensions/packages/{id}/images/{index}` | A marketplace image. |
| `GET /stage/extensions/packages/{id}/placeholders/{component}` | A component's placeholder. |
| `GET /stage/extensions` | The listing. |
| `GET /stage/extensions/view` | The listing as a page. |

`{id}` is the digest without `sha256:`. The routes under `/stage/extensions` are tooling: they are served in the sandbox and not by a published application.

**Placeholders cannot run anything.** A placeholder is served with a content security policy that forbids script and external loads, and it is its own sandboxed
origin even when opened on its own:

```text
Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'self'; sandbox
X-Content-Type-Options: nosniff
```

A request that a browser makes as a page (`Sec-Fetch-Dest` other than `iframe`, `frame` or `empty`) is refused with 403, so a placeholder is only shown inside a frame.
The icon and images are served with a policy that lets an SVG draw and nothing else.

## The listing

`GET /stage/extensions` lists what is loaded and what was refused. It is the endpoint a design tool such as Studio and a future marketplace read.

```json
{
  "packages": [
    {
      "id": "13c80dcd…",
      "reference": "ghcr.io/cratis/extension-mail",
      "version": "2.1.0",
      "digest": "sha256:13c80dcd…",
      "publisher": "https://github.com/cratis",
      "channel": "oci",
      "signatureVerified": true,
      "displayName": "Transactional mail",
      "description": "Sends invoices and receipts through a transactional mail service",
      "icon": "/stage/extensions/packages/13c80dcd…/icon",
      "images": [ { "url": "/stage/extensions/packages/13c80dcd…/images/0", "caption": "The inbox" } ],
      "requirements": [ { "kind": "stage", "value": "^5" } ],
      "contents": {
        "abilities": [ "Send" ],
        "triggers": [ "Delivered" ],
        "components": [ { "name": "MailPreview", "kind": "react", "platforms": [ "web" ], "placeholder": "/stage/extensions/packages/13c80dcd…/placeholders/MailPreview" } ],
        "settings": [ { "name": "apiKey", "exposure": "secret" } ],
        "backend": { "runtime": "dotnet", "version": "10" },
        "hasFrontend": true,
        "activation": "on demand"
      }
    }
  ],
  "refused": [ { "reference": "acme/ledger", "reason": "NotAllowed", "details": "…" } ]
}
```

Settings are listed by name and exposure only; a value never leaves the host. `/stage/extensions/view` renders the same listing as a page with no script.

## Configuration

`Stage:Extensions:Packages` binds to `ExtensionPackageOptions`.

| Option | Default | Meaning |
| --- | --- | --- |
| `CacheDirectory` | `Cratis/Stage/extensions` in the local application data folder | Where downloads are kept, by digest. |
| `Allow:Publishers`, `Allow:References` | none | What the deployment allows. Narrows the project. |
| `Publishers:<publisher>:Keys` | none | Signing keys of a publisher. Replaces the project's keys for that publisher. |
| `RequireSignatures` | `true` | Whether a package must be signed. Only the deployment can change it. |
| `Registries:<host>:Username`, `Password` | none | Credentials for a private registry, by registry host. A bearer token for npm is the password with an empty user name. They come from configuration and never from the model or the lock. |
| `NpmRegistry` | `https://registry.npmjs.org` | The npm registry. |
| `MaxPackageBytes` | 256 MiB | The most a package may unpack to. |
| `MaxPackageFiles` | 10 000 | The most files a package may hold. |
| `RequestTimeout` | 60 seconds | How long one request to a registry may take. |

Only a registry on loopback may be reached without TLS.

## What loading does not do

Loading reads and verifies packages. It does not run a backend, choose how it runs or start it when something needs it; that is the work of
execution tiers and lazy activation (Cratis/Stage#1066), which build on the loaded packages: `IExtensionPackages` tells which package a system's extension is,
and `LoadedExtensionPackage.UnpackBackend` unpacks its backend from bytes that are verified against the locked digest again first. Vendoring a package into the
model folder is `screenplay restore --vendor`.

## Related

- [Extension backends](index.md) and the [protocol reference](protocol.md).
- [Connect a backend to Stage](connect-a-backend.md) for the configuration of a backend that runs by itself.
- Screenplay: [extension contracts, the manifest and the lock](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md),
  [the command line](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#restore-extensions) and
  [systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md).
