---
title: From a model to a running extension
description: Where Cratis Stage gets its ability catalog, component catalog and settings from - the compiled model and the locked packages - and the Notifier sample that runs from a lock, starts when its screen is reached and answers an invocation durably.
---

A model says which systems exist, which abilities they offer and which screens place which components. A locked package says what the extension
really is: its components, what each may invoke, the signals it raises, its settings and its backend. Stage joins the two when it starts, and
what the extension hosting answers from afterwards is that join, not a list somebody configured.

## What comes from where

| Stage asks | Answered from | By |
| --- | --- | --- |
| Which abilities can be invoked, with which effect and outcomes | The model's systems that a loaded package realizes, whose abilities Screenplay binds from the package's contract | `ExtensionModelCatalogs.Abilities` |
| Which component a screen places, and what it may invoke and raise | The [UI contract](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/screens.md) of the model (`scene.components[].extension`) and the package's contract | `ExtensionModelCatalogs.Placements` |
| Which backends a screen needs | The UI contract's `extensions` of each screen | `ExtensionModelCatalogs.Usage` |
| Which settings a system has, and which a browser may read | The package's contract. Only `public` reaches a browser; `secret` and `backend` do not | `ExtensionModelCatalogs.Settings` |
| Which package an ability, a component or a backend belongs to | The system's `extension` reference, matched to the lock entry that pins it | `PinnedAbilityCatalog` |
| The element the browser loads for a placed component | The scene Stage builds from the same model, with the package's frontend, abilities and signals added | `ExtensionSceneComponents` |

`SemanticHost` does this once the packages are loaded and the model has compiled, and it is what turns the empty catalogs the extension hosting
starts with (see [Run extension components](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/components.md)) into the model's.

A system that no loaded package realizes has no abilities in the catalog, so an invocation of it is refused rather than started. A component the
package's contract does not declare is not placed, so asking for it answers `404`.

Compiling the model needs the lock. Stage looks for `screenplay.lock` beside the model, not beside the directory it was started from, and compiles
the model against the contracts of the packages it loaded.

## The Notifier sample

[`Samples/Extensions/Notifier`](https://github.com/Cratis/Stage/tree/main/Samples/Extensions/Notifier) is a minimal extension with everything the
path needs:

| File | What it is |
| --- | --- |
| `contract.play` | The extension's contract: the `Notify` ability (`once`, with a `Muted` outcome), the `NoticeBoard` React component that may invoke `Notify` and raises `dismissed`, a public `channel` setting and a secret `token`. |
| `frontend/index.js` | The component, built on `@cratis/scene.extensions`. React and the extensions package come from Stage's import map. |
| `placeholders/notice-board.html` | The static placeholder design tools draw instead of the component. |
| `backend/` | The backend, written with the [C# SDK](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/csharp.md). |

A model uses it by naming the package on a system and placing the component on a screen:

```screenplay
system Notifier
  description "Posts notices to a chat channel"
  extension "ghcr.io/acme/extension-notifier"

screen NoticeBoard
  title "Notices"
  component Notifier.NoticeBoard board
    property title = "Today"
```

The specifications in `Source/Host/Extensions/for_ExtensionEndToEnd` run that, with a real Orleans silo and the gRPC protocol, over the sample's
own files:

1. **Loaded from a lock.** The sample is packed, signed and loaded through the package loader from a `screenplay.lock` entry, and the model
   compiles against its contract.
2. **Lazy activation.** Nothing is started when Stage starts. Reaching the `NoticeBoard` screen starts the `Notifier` backend, once.
3. **Invoked from a screen.** The component's `invoke` goes through the browser gateway: the screen must place the component, the contract must
   allow the ability and the person must be signed in. The invocation is recorded with the screen interaction as its cause and pinned to the locked
   package.
4. **Answered durably.** The backend answers, the invocation settles in the ledger with the outcome and output, and asking again with the same
   interaction finds that invocation instead of doing the effect twice.

The backend in these specifications is a loopback gRPC server that speaks the same protocol and declares the same ability as the sample's backend,
because the Stage host project and the SDK each generate the protocol types. The sample's backend is built with the solution, and the
[conformance suite](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/conformance.md) holds each SDK to the protocol.

## In the browser

When a component is placed, the page asks Stage to start its system's backend before the component has loaded, with
`POST /stage/extensions/activation/screens/<screen>`. It carries nothing but the screen and a failure does not stop the component loading: the
first invocation starts the backend anyway. See [Execution tiers](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/execution-tiers.md) for what starting means.

## Not done yet

- **Signals.** The element carries the signals the component may raise, and the runtime reports them to the page, but a screen's
  `on <signal>` interaction is not bound to them. That waits for Scene's signal trigger; the UI contract already lists the handlers
  (`scene.components[].extension.handles`), so binding them changes the runtime and not this wiring.
- **Screen authorization.** A placement can name the policy of its screen, but the UI contract does not name one yet, so a browser invocation
  needs a signed-in person and nothing more. The ability's own `authorize` still applies.
- **Setting values from the model.** The values behind `setting <name> = ...` and `setting <name> from secret ...` on a system are not read from the
  model yet; settings resolve from configuration under `systems:<System>:<setting>`.
- **Strings.** `$strings` references in a component's properties are not resolved into the element's `strings`.
