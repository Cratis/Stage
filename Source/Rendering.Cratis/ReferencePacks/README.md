# Embedded reference pack manifests

The renderer embeds the top-level DLLs from `Microsoft.NETCore.App.Ref` 10.0.12
(`ref/net10.0`) and 11.0.0 (`ref/net11.0`). These manifests pin that exact file
set and its SHA-256 hashes. XML documentation and other files are not embedded
and are not part of the audited assembly set.

`ReferencePacks.targets` verifies both packs before resource assignment on every
build, including incremental builds. A missing, altered, or additional DLL fails
the build with its file name. Missing, empty, or malformed manifests also fail
closed. The build task's source is compiled directly into the focused
`Source/ReferencePacks.Specs` project so its comparison logic is tested without
building or running the renderer's full specification suite.

## Updating a pack version

Update the pins in `Directory.Packages.props` and the corresponding admission
constants, restore the packages from a trusted source, and regenerate the
manifest from the resolved NuGet package directory (substitute your NuGet cache
path if it is not the default):

```sh
node Source/Rendering.Cratis/ReferencePacks/generate-manifest.mjs ~/.nuget/packages/microsoft.netcore.app.ref/10.0.12 10.0.12 net10.0
node Source/Rendering.Cratis/ReferencePacks/generate-manifest.mjs ~/.nuget/packages/microsoft.netcore.app.ref/11.0.0 11.0.0 net11.0
```

The generator writes sorted relative file names and lowercase hexadecimal hashes.
Review and check in the regenerated manifests with the version change. Do not
regenerate a manifest to silence a mismatch in an existing pinned pack: repair
the extracted package instead. Run the Release renderer build and the focused
reference-pack specifications before committing.
