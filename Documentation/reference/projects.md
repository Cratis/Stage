---
title: Cratis Stage project folders
description: Cratis Stage merges every .play file beneath a project folder into one application, with shared declarations and deterministic discovery order.
---

Cratis Stage treats a project folder as one application. Use a folder root when your model spans several `.play`
files; declarations in those files resolve together. Give separate applications separate roots, because every
`.play` file beneath a root participates in its application.

```bash
stage ./eventmodel          # the folder, as one application
stage ./eventmodel/app.play # one file, on its own
```

The Host uses the semantic engine by default. Pass `--engine=eventmodel` to the Host executable or set
`Stage__Runtime__Engine=eventmodel` to keep the deprecated legacy engine. It logs one warning at startup and
will be removed in Stage v6. The SpecRunner retains `structural` as its default.

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

Choose folders to help people find the model: one module per folder, a feature tree or a split by slice all
work. Names resolve through the declared module and feature tree, never through the directory tree. A contribution
attaches to the nearest enclosing structure declaring a matching contribution point. A behavior attached to a
module applies to every screen beneath it, regardless of which file declares it.

Ordinal path ordering makes diagnostics and generated output deterministic. It gives no declaration precedence
over another: an earlier file does not win. If changing a file name changes the document's meaning, that is a bug.

## Declared once, wherever it lives

Some declarations are singular for the whole application. The compiler enforces that across files and names both
ends when it fails — the file that already claimed the name, and the location that tried to claim it again:

```text
second.play(1,1): error PLAY0172: The folder already declares a domain in 'first.play' —
                  a folder compiles to one application, which can have at most one
```

Singular across the whole folder: `domain`, `layout`, `theme` and `ui profile`. Within a `ui profile`,
`blueprint` and `start screen` are each at most one.

Everything else merges: modules combine, features and slices join their module, imports are merged and
de-duplicated, and contributions from anywhere in the tree aggregate into the point they target.

## The one hazard

Two unrelated applications in one folder will be merged. You may see a duplicate-declaration error naming files
from both applications, or a navigation item from an application you did not intend to run. Move each application
to its own folder so discovery cannot combine them.

The same applies to a stray `.play` file left beneath the root: a scratch file, a half-finished experiment, a
copy saved "just in case". If it is beneath the folder, it is part of the application.

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
