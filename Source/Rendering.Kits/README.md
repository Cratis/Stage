<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Target kits

`Cratis.Stage.Rendering.Kits` loads target-independent rendering kits without executing their templates.
The entry point is `KitLoader.Load(IKitSource, KitEngineCapabilities, KitLoadLimits?)`.
A successful result contains an immutable manifest and files, canonical uncompressed ustar bytes,
and their lowercase SHA-256 digest. A rejected result contains no kit and diagnostics ordered by
code, ordinal path, then message. No partial kit is returned.

## Manifest

Every kit contains `stage-kit.json`. All nine root members are required, including empty arrays:

```json
{
  "apiVersion": "stage-target/v1.0",
  "target": { "id": "axon", "version": "1.0.0" },
  "languagePacks": ["java@1.0.0"],
  "requires": ["model/v1.0"],
  "outputs": [
    { "id": "main", "template": "templates/main.sbn", "path": "{{ name }}.java" }
  ],
  "partials": [],
  "tables": [],
  "assets": [],
  "diagnostics": []
}
```

`outputs` declare `id`, `template`, and opaque output `path` expressions, with optional opaque
`forEach` and `when` expressions. Partials declare `name` and `template`; tables declare `name`
and `path`; assets declare a source `path` and opaque `output`; diagnostics declare `code` and
`summary`. Unknown members, duplicate JSON properties, missing values, and required null values
are rejected. Diagnostic codes must be uppercase, hyphen-separated identifiers outside `STAGE-*`.
The embedded [JSON schema](stage-target.v1.schema.json) is generated from the serialization contract
by `KitManifestSchema.Generate()`; the drift spec keeps the checked-in copy synchronized.
Compatibility, file safety, uniqueness, and availability checks are performed by the loader, not
by the structural schema.

## Sources and compatibility

Use `InMemoryKitSource`, `DirectoryKitSource`, or `KitArchive.Read(bytes)`.
Pass the engine's exact available language-pack identities and provided contract versions in
`KitEngineCapabilities`. Pack bindings are exact (no ranges or build metadata). Contract requirements
need the same major and a provided minor greater than or equal to the requested minor. The API
is `stage-target/v1.<minor>`, with the requested minor no newer than the engine's supported minor.

Kit-relative paths use only ASCII letters, digits, `/`, `.`, `_`, and `-`; they cannot be rooted,
contain empty/dot/parent segments, collide case-insensitively, or exceed portable ustar limits.
Declared text is strict UTF-8 without a BOM; assets may contain arbitrary bytes. Undeclared files,
links (including directory roots and ancestors), special archive entries, and configured resource
limit violations are rejected. Defaults are 2,048 files, 8 MiB per file, and 64 MiB total.
Directory sources assume a stable tree while loading; they are not a sandbox for concurrent
adversarial filesystem mutation. After loading, no further source access is needed.

`KitArchive.Write(files)` sorts paths ordinally and fixes permissions to `0644`, owner and group
IDs to zero, names to empty, and modification times to the Unix epoch. It excludes explicit directory
entries, compression, and platform metadata. `KitDigest.Compute(bytes)` hashes these archive bytes,
including the manifest, so file enumeration order does not affect the loaded kit's identity.
