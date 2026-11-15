---
title: Cratis Stage project folders
description: Cratis Stage merges every .play file beneath a project folder into one application, with shared declarations and deterministic discovery order.
---

Cratis Stage treats a project folder as one application. Use a folder root when your model spans several `.play`
files; declarations in those files resolve together. Give separate applications separate roots, because every
`.play` file beneath a root participates in its application.

To run a project locally, [install the Cratis CLI](/cli/getting-started/#install-it) and meet the
[`cratis run` prerequisites](/cli/reference/run/#prerequisites): Docker installed and the `docker` command on your
`PATH`. Then point `cratis run` at the folder or at a single file:

```bash
cratis run ./eventmodel          # the folder, as one application
cratis run ./eventmodel/app.play # one file, on its own
```

Without a path, `cratis run` uses the current folder. The CLI mounts only the selected folder or file into the
Stage container, read-only, and publishes the Stage API on `http://localhost:9090`.

The Host uses the semantic engine by default. Pass `--engine=eventmodel` to the Host executable or set
`Stage__Runtime__Engine=eventmodel` to keep the deprecated legacy engine. It logs one warning at startup and
will be removed in Stage v6. With the CLI, pass `--engine eventmodel` to `cratis run`. The SpecRunner retains
`structural` as its default.

## What Stage does with the folder

1. Discovers every `.play` file beneath the folder recursively with `**/*.play`. Files are ordered by relative
   path using an ordinal comparison, so discovery has the same order on every machine.
2. Parses each file on its own.
3. Merges the files into one application before resolving references.
4. Resolves the merged application, then visits it to produce the event model and the Scene application.

Because the merge happens before resolution, a declaration in one file and its use in another simply work. An
event declared in `events.play` and produced by a command in `invoicing/register.play` resolves. A concept
declared once at the root is available to every slice. A policy an `authorize` names is found wherever it lives.

## File layout carries no meaning

Choose folders to help people find the model: one module per folder, a feature tree or a file per slice all
work. Names resolve through the declared module and feature tree, never through the directory tree. A contribution
attaches to the nearest enclosing structure declaring a matching contribution point. A behavior attached to a
module applies to every screen beneath it, regardless of which file declares it.

Ordinal path ordering makes diagnostics and generated output deterministic. It gives no declaration precedence
over another: an earlier file does not win. If changing a file name changes the document's meaning, that is a bug.

## Declared once, wherever it lives

The merge sorts declarations into three groups.

**Singular for the whole folder.** A folder compiles to one application, so it has at most one `domain`, one
`authentication`, `identity`, `onboarding` and `context` block, and one design system. A second file that declares
one of these is an error that names the file that already claimed it:

```text
second.play(1,1): error PLAY0172: The folder already declares a domain in 'first.play' - a folder compiles to one application, which can have at most one
```

**Combined by name.** `module` and `feature` declarations with the same name are the same container, wherever
they are written. Every file that names `module Invoicing` adds to one module, and every file that names
`feature Invoices` inside it adds to one feature. That is what lets a slice live in its own file and still belong to
its feature. Unquoted contract imports are merged and de-duplicated, seeds accumulate, and contributions from
anywhere in the tree aggregate into the point they target.

**Distinct names accumulate; a repeated name is rejected.** Named declarations such as `concept`, `type`,
`policy`, `persona`, `layout`, `theme` and `ui profile` can sit in different files as long as their names differ.
Inside a feature, the same holds for slices, screen templates, dialog templates and forms. Declaring a name that
another file already claimed is an error, and the second declaration is dropped:

```text
second.play(1,1): error PLAY0173: Duplicate declaration of 'InvoiceId' - already declared in 'first.play'
second.play(3,5): error PLAY0173: Duplicate slice 'Register' in feature 'Invoices' - already declared in 'first.play'
```

### A slice lives in one file

Splitting one slice across files is not supported. Repeating a `slice` with the same name in the same feature from
another file does not extend the first one; it is the `PLAY0173` duplicate above. Keep every part of a slice in one
file, and use several files for several slices of the same feature.

A `ui profile` is also complete in one place: it names at most one `theme`, one `layout` and one `root template`.

Duplicates within a single file are reported by the single-document compiler under its own rules. The
[Screenplay folder reference](/screenplay/folders/#how-the-files-become-one-application) lists the merge rule for
every declaration.

## The one hazard

Two unrelated applications in one folder will be merged. You may see a duplicate-declaration error naming files
from both applications, or a navigation item from an application you did not intend to run. Move each application
to its own folder so discovery cannot combine them.

The same applies to a stray `.play` file left beneath the root: a scratch file, a half-finished experiment, a
copy saved "just in case". If it is beneath the folder, it is part of the application.

## When a project is refused

A Stage that answers HTTP is not necessarily running your project. Stage reports the project's state at
`/stage/status`:

| `state` | Meaning |
|---|---|
| `warm` | The host is listening and waiting to be handed a model; no project runs yet. |
| `loading` | The project is loading and is not accepted yet. Add `?wait=<seconds>` (at most 300) to hold the request until loading ends or the time runs out. |
| `ready` | The project compiled and the session accepts commands and queries. |
| `unsupported` | The project was refused. `issues` says why; nothing from the model runs. |

Each entry in `issues` carries a `capability`, an `artifact` and `details`. A model that does not compile, such as
a folder with a duplicate slice, is reported under the `Plan` capability with one entry per compiler error in
`details`. A path with no `.play` files is reported the same way. A sandbox host stays up after a refusal, so check
`state`, not whether the port answers:

```bash
curl http://localhost:9090/stage/status
```

`cratis run` reads the same endpoint for you. It waits through `warm` and `loading`, and when Stage reports
`unsupported` it prints the refusal grouped by kind, stops the container and exits with code `5`. Pass `--verbose`
to list every issue and stream the container's output.

To recover:

1. Read the issues from the `cratis run` output, or from `/stage/status` on a host you started yourself.
2. Correct the reported source, or remove the capability the engine does not support yet.
3. Start the project again. Stage reads the model when it starts and does not watch the folder, so a fix takes
   effect only after a restart.

## Body worker process

`Cratis.Stage.BodyWorker` is Stage's separate .NET process for compiled rule units. The
`BodyWorker` project builds it; Host publishes it into a `bodyworker/` subdirectory.
Its `--role compile` process emits admitted units, while `--role execute` evaluates them
through versioned stdin/stdout messages. Neither role opens a network listener.

The worker isolates accidental loops, crashes and excessive allocation in trusted local
model code. The execute role has a managed heap limit plus a working-set watchdog; this
is not an OS memory cap or a security boundary for hostile multi-tenant code.

### Reusing body execution for an on-disk root

`Cratis.Stage.Contracts.Execution.ModelRootBodyExecution` implements
`ISemanticBodyExecutionProviderFactory` for a model folder. Construct it with the root
and `BodyWorkerOptions`, and dispose it asynchronously when the host shuts down.
Construction starts background compilation; the first `CreateAsync` awaits it.
Unchanged roots reuse the manifest and execute worker. Changed files trigger one new
compilation; existing runs retain their previous worker until they are disposed.

The factory reads an existing `.screenplay/identities.json` without modifying it.
Its change fingerprint uses relative paths, lengths and UTC modification times. It
ignores `.git`, `.ai-work`, `bin`, `obj`, `node_modules`, `.screenplay/notes.json`
and `.screenplay/release-baseline.json`. Compile failures become unsupported-body
diagnostics rather than escaping from run creation.

`BodyWorkerLaunch.ForCurrentProcess(verb)` relaunches a host with its hidden worker
verb. It prepends the entry assembly only when the executable is `dotnet`; apphosts
and installed tools receive the verb directly. The host must dispatch that verb to
its worker implementation.

## See also

- [URLs of a running Stage](urls.md) shows what the session exposes once the folder has compiled.
- [Screenplay folders](/screenplay/folders/) specifies the merge rules declaration by declaration.
- [`cratis run`](/cli/reference/run/) covers every option, the refusal output and running in the background.
