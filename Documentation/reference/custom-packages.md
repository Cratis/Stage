---
title: Cratis Stage custom runtime packages
description: Approve custom components and blueprints for Cratis Stage, then load them through generated frontend dependencies or a live Stage package folder.
---

An approved package set tells Cratis Stage which custom components and blueprints an application may use.
A `ui profile` lists the packages its component names resolve against. Stage's built-in catalog,
`CratisRendering.ShippedPackages`, contains `core`, `scene.web`, `Cratis.Components`, `PrimeReact` and
`Cratis.Blueprint.Default`. Approve any package outside that catalog before rendering a profile that names it.

Put the manifest and browser module declaration in a Scene [approved package set](https://github.com/Cratis/Scene/blob/main/Documentation/package-hosting.md#approved-package-sets-for-custom-runtime-packages),
which `Cratis.Scene.Engine.Packages.ApprovedPackageSetJson` reads and writes. Pass the document with
`cratis render --package-set <file>`, or supply it directly to the planner from a host. For example, this set
approves the `chart` component from `Acme.Charts` and identifies the npm module that provides it:

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

`CratisRendering.CatalogWith(set)` combines the shipped packages with the approved manifests. The same
`RenderPlanner` checks layouts, themes, dependencies and version ranges for both. A package in neither list
produces `PackageNotInCatalog`.

`CratisRendering.CreateProfile(applicationName, options, scene, stringsFiles, defaultLocale, approvedPackages)`
carries the set as the scaffold input `approved-packages.json` and generates the frontend that installs and
loads it. Without approved packages, the plan's bytes are unchanged.

Stage refuses a malformed set, a set that reuses a shipped package name, or a set that differs from the one used
to generate the scaffold, with the typed profile diagnostic `STAGE-CRATIS-001`. During admission, Stage regenerates
the whole frontend from the document and compares every byte, including `package.json` and the loader.

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

A generated application installs approved packages as frontend dependencies and includes them in its build.
The `cratis/stage` image cannot install npm packages. For a live Stage, including `cratis run`, supply the same
approved package set together with each package's browser build. Stage serves those modules to its own page.

The Stage reads a folder (`Stage:Packages:Path`, `/packages` in the image) laid out like `node_modules`:

```text
/packages/approved-packages.json
/packages/@acme/scene.charts/package.json     exports / module / main (or index.js) names the browser module
/packages/@acme/scene.charts/index.js         an ES module exporting the bundle named by module.export
/packages/@acme/scene.charts/styles.css       every style sheet the set lists, relative to the package
```

`cratis run --package-set <file> [--packages <folder>]` mounts the set and each approved package's folder read-only at those
paths ([run reference](/cli/reference/run/)). The Stage:

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

The shipped package declarations come from the real package manifests. `Source/Frontend/src/shippedPackages.spec.ts`
checks them against `Rendering.Cratis/Shipped/ShippedPackages.json`, so the catalog used to resolve a `ui profile`
matches the frontend's packages.
A domain blueprint - `@cratis/scene.blueprint.crm`, `.ticketing`, `.approvals`, `.inventory`, `.hr`, `.project-tracker`, `.administration`, `.orders`, `.catalog`, `.vendors`, `.invoicing`, `.customer-portal`, `.contracts`, `.work-orders`, `.bookings` - therefore
only needs approving itself: its dependency on `Cratis.Blueprint.Default` is already satisfied.

The approved set accepts a blueprint manifest as published. Its `blueprint` and `sampleData` members are design-time only and are dropped from the
canonical document, so sample data never reaches a generated application or `GET /stage/packages` (spec SD6). The screen and dialog templates
of an approved blueprint are what an authored `template blueprint <Package> <Template>` reference fills, in a live Stage and in a generated
application. A blueprint bundle imports `@cratis/scene.blueprint.default`, `@cratis/scene.engine`, `@cratis/scene.model` and `@cratis/scene.react`; a live Stage
maps them to its own instances, like React.

For how Stage chooses forms and lookups when you have not authored a screen, see
[Default generated Scene](default-scene-composition.md).
