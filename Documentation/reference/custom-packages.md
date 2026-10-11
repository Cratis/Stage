---
title: Custom runtime packages
description: Approve component packages beyond the ones Stage ships, so a rendered Cratis application renders a ui profile that names them - the approved package set, what planning and admission check, what the generated frontend installs and loads, and what a live Stage does not do.
---

A `ui profile` lists the packages its component names resolve against. Stage ships three: `core`, `scene.web` and
`Cratis.Components` (`CratisRendering.ShippedPackages`). Any other package is not renderable until the application
**approves** it. Approval is one document, the Scene [approved package set](https://github.com/Cratis/Scene/blob/main/Documentation/package-hosting.md#approved-package-sets-for-custom-runtime-packages),
that `Cratis.Scene.Engine.Packages.ApprovedPackageSetJson` reads and writes. The `cratis` CLI passes it with
`cratis render --package-set <file>`; a host calls the planner directly.

```json
{
  "version": 1,
  "packages": [
    {
      "manifest": { "name": "Acme.Charts", "version": "1.2.0", "kind": "ComponentLibrary", "components": ["chart"] },
      "module": { "npm": "@acme/scene.charts", "version": "1.2.0", "export": "acmeChartsRuntimePackage", "styles": ["@acme/scene.charts/styles.css"] }
    }
  ]
}
```

## What planning does

- `CratisRendering.CatalogWith(set)` is the catalog a `ui profile` is resolved against: the shipped packages followed by the approved
  manifests. A profile naming an approved package resolves like a shipped one - its layouts, themes, dependencies and
  version ranges are checked by the same `RenderPlanner`. A package in neither list is still `PackageNotInCatalog`.
- `CratisRendering.CreateProfile(applicationName, options, scene, stringsFiles, defaultLocale, approvedPackages)` carries the set in
  the render profile as the scaffold input `approved-packages.json`, and generates the frontend that installs and loads it.
  An application with no approved packages plans byte-for-byte what it planned before.
- The set is refused, with the typed profile diagnostic `STAGE-CRATIS-001`, when it is malformed, reuses the name of a
  shipped package, or is not exactly what the scaffold was generated from: admission regenerates the whole frontend from the
  document and compares every byte, so a profile cannot smuggle in a different `package.json` or loader.

## What the rendered application contains

For an application with an approved set, the planned artifacts add and change:

| Artifact | Content |
| --- | --- |
| `approved-packages.json` | The canonical document. |
| `package.json` | One extra dependency for each `module.npm` at `module.version`, in ordinal order among the others. A package that is already installed (for example `react`) or reuses a shipped package name is refused. |
| `.frontend/approvedPackages.ts` | A literal `import()` for each approved module, the package style sheets, and `loadApprovedComponents()`, which loads the bundles through `loadApprovedPackages` from `@cratis/scene.react`. |
| `.frontend/main.tsx` | Awaits `loadApprovedComponents()` before the first render and renders `App` with the approved components next to the Cratis components. |

`loadApprovedPackages` refuses a bundle that differs from its declaration - name, version, kind, components, layouts, templates
or themes - and a package that fails to import or lacks its export. The application then stops with the reasons on the page, never
rendering a profile with a package silently missing. Component names keep their package: a registered `Acme.Charts:chart` is
what the authored `component Acme.Charts.chart` resolves to.

## What a live Stage does not do

The `cratis/stage` image is a built frontend. A runtime cannot install an npm package into it, and Stage does not load
arbitrary modules from a mounted folder, so a **live** Stage (and therefore `cratis run`) renders only the shipped packages. Custom
packages are rendered by the application you generate and build: `cratis render --package-set ...`, then `npm install` and
`npm run build`. Use the live Stage to review a model that stays within the shipped packages.
