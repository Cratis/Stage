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
| `.frontend/main.tsx` | Awaits `loadApprovedComponents()` before the first render and renders `App` with the approved components - and the screen and dialog templates the approved packages bring - next to the Cratis components. |

`loadApprovedPackages` refuses a bundle that differs from its declaration - name, version, kind, components, layouts, templates
or themes - and a package that fails to import or lacks its export. The application then stops with the reasons on the page, never
rendering a profile with a package silently missing. Component names keep their package: a registered `Acme.Charts:chart` is
what the authored `component Acme.Charts.chart` resolves to.

## A live Stage loads the packages at runtime

A generated application builds its approved packages in. The `cratis/stage` image is a built frontend that cannot install npm
packages, so a **live** Stage (and `cratis run`) loads them at runtime instead: it is handed the same approved package set and the
browser build of each package, and serves them to its own page.

The Stage reads a folder (`Stage:Packages:Path`, `/packages` in the image) laid out like `node_modules`:

```text
/packages/approved-packages.json
/packages/@acme/scene.charts/package.json     exports / module / main (or index.js) names the browser module
/packages/@acme/scene.charts/index.js         an ES module exporting the bundle named by module.export
/packages/@acme/scene.charts/styles.css       every style sheet the set lists, relative to the package
```

`cratis run --package-set <file> [--packages <folder>]` mounts the set and each approved package's folder read-only at those
paths ([run reference](https://github.com/Cratis/cli/blob/main/Documentation/reference/run.md)). The Stage:

- reads the set with `ApprovedPackageSetJson` (so a malformed set, or one that reuses a shipped package name, is refused) and serves it, with
  where each module is served from, at `GET /stage/packages`;
- serves only the files inside the folders of the packages the set names, and only types a package ships (`.js`, `.mjs`, `.css`, images,
  fonts, source maps) from `/stage/packages/files/**` - never the set document or any other folder;
- before the first render loads each module with `import()` from the URL it reported - never an address a scene supplied - through the
  same `loadApprovedPackages` a generated application uses. A package that is missing, does not load, lacks its export or differs from its
  declaration stops the Stage with the reasons on the page; a profile is never rendered with a package missing.

A package is a browser ES module that leaves React external. Its `import ... from 'react'` (and `react/jsx-runtime`, `react-dom`,
`react-dom/client`) resolves through an import map in the Stage page to shim modules that re-export **the Stage's own React**, so the
package's hooks and contexts are the Stage's. A package that bundles its own copy of React fails on its first hook. Anything else a
package imports must be bundled into its module. A Stage handed no set renders the five shipped packages, as before.

## Domain blueprints

The Stage ships `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and `Cratis.Blueprint.Default`; their declarations are generated
from the real package manifests by `Source/Frontend/src/shippedPackages.spec.ts` (which fails when the checked-in
`Rendering.Cratis/Shipped/ShippedPackages.json` is stale), so the catalog a `ui profile` is resolved against is the one the frontend renders with.
A domain blueprint - `@cratis/scene.blueprint.crm`, `.ticketing`, `.approvals`, `.inventory`, `.hr`, `.project-tracker`, `.administration`, `.orders`, `.catalog`, `.vendors`, `.invoicing`, `.customer-portal`, `.contracts`, `.work-orders`, `.bookings` - therefore
only needs approving itself: its dependency on `Cratis.Blueprint.Default` is already satisfied.

The approved set accepts a blueprint manifest as published. Its `blueprint` and `sampleData` members are design-time only and are dropped from the
canonical document, so sample data never reaches a generated application or `GET /stage/packages` (spec SD6). The screen and dialog templates
of an approved blueprint are what an authored `template blueprint <Package> <Template>` reference fills, in a live Stage and in a generated
application. A blueprint bundle imports `@cratis/scene.blueprint.default`, `@cratis/scene.engine`, `@cratis/scene.model` and `@cratis/scene.react`; a live Stage
maps them to its own instances, like React.
