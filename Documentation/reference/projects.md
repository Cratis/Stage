---
title: Projects — what a folder means to Stage
description: How Stage turns a folder of .play files into one application — discovery, merge order, what must be declared only once, and the one hazard to know about.
---

Point Stage at a folder and it runs **one application**. Not one per file, and not a set of applications that
happen to share a directory — the folder *is* the unit.

To start a local sandbox, [install the Cratis CLI](/cli/getting-started/#install-it) and have
[Docker installed and running](/cli/reference/run/#prerequisites), with `docker` on your `PATH`.

The paths below are examples: replace `./eventmodel` with your existing folder of `.play` sources, or
`./eventmodel/app.play` with a single `.play` file. If you need to write one, start with
[Screenplay's modeling walkthrough](/screenplay/getting-started/#model-a-slice).

Run only models you trust, on a trusted network. The CLI publishes ports `9090` and `35000` without a
host-IP restriction; the `localhost` URLs below do not mean the sandbox is reachable only from your machine.

```bash
cratis run ./eventmodel          # the folder, as one application
cratis run ./eventmodel/app.play # one file, on its own
```

The CLI mounts the selected input read-only and publishes the Stage API on `http://localhost:9090`.
Once the model is ready, open `http://localhost:9090/scalar/v1` to try its commands and queries.

The Host uses the semantic engine by default. Pass `--engine=eventmodel` to the Host executable or set
`Stage__Runtime__Engine=eventmodel` to keep the deprecated legacy engine. It logs one warning at startup and
will be removed in Stage v6. The SpecRunner retains `structural` as its default.

Missing input or a folder with no `.play` files can stop startup before a host is available. For a model
refusal, `cratis run` prints a summary, stops the container and exits with code `5`. Read that summary, or
rerun with `--verbose` to see every issue and the container output. Correct invalid source or address the
reported unsupported capability, then rerun.

For a Host that is still running, such as one started directly, `/stage/status` reports `loading`,
`unsupported` or `ready` with the semantic engine. A listening port alone does not mean the model was
accepted; inspect `issues` when the state is `unsupported`.

## Body worker process

`Cratis.Stage.BodyWorker` is Stage's separate .NET process for compiled rule units. The
`BodyWorker` project builds it; Host publishes it into a `bodyworker/` subdirectory.
Its `--role compile` process emits admitted units, while `--role execute` evaluates them
through versioned stdin/stdout messages. Neither role opens a network listener.

The worker isolates accidental loops, crashes and excessive allocation in trusted local
model code. The execute role has a managed heap limit plus a working-set watchdog; this
is not an OS memory cap or a security boundary for hostile multi-tenant code.

## What Stage does with the folder

1. **Discovers** every `.play` file beneath the folder, recursively — the glob is `**/*.play`, so nesting is
   free. Files are ordered by relative path using an ordinal comparison, which makes discovery identical on
   every machine.
2. **Parses** each file on its own.
3. **Merges** them into the one application they describe — *before anything is resolved*. This is the step that
   matters.
4. **Resolves** the merged application, then visits it to produce the event model and the Scene application.

Because the merge happens before resolution, a declaration in one file and its use in another simply work. An
event declared in `events.play` and produced by a command in `invoicing/register.play` resolves. A concept
declared once at the root is available to every slice. A policy an `authorize` names is found wherever it lives.

## File layout carries no meaning

The folder structure is for humans. Put one module per folder, mirror your features, split by slice — Stage does
not care, and neither does resolution:

- **Names resolve through the module and feature tree,** never through the directory tree. A contribution
  attaches to the nearest enclosing structure declaring a matching contribution point; a behavior attached to a
  module applies to every screen beneath it. Which file any of that was written in is irrelevant.
- **Merge order is not precedence.** Ordinal path ordering exists so that diagnostics and generated output are
  deterministic, not so that an earlier file wins. If the meaning of a document ever seems to depend on a file
  name, that is a bug, not a feature to rely on.

## Declared once, wherever it lives

Some declarations are singular for the whole application. The compiler enforces that across files and names both
ends when it fails — the file that already claimed the name, and the location that tried to claim it again:

```text
second.play(1,1): error PLAY0172: The folder already declares a domain in 'first.play' —
                  a folder compiles to one application, which can have at most one
```

A `domain` is singular across the whole folder. Named `layout`, `theme` and `ui profile` declarations
combine when their names differ; repeating the same name in another file is an error.

Modules and features combine by name within their enclosing scope. Each slice belongs to one file:
declaring the same slice name in the same feature from another file is a duplicate, not a way to extend it.
Imports are merged and de-duplicated, and contributions from anywhere in the tree aggregate into the point
they target.

## The one hazard

**Two unrelated applications in one folder will be merged**, because nothing in the folder says they are
separate. The symptom is usually a duplicate-declaration error naming two files you did not think were related —
or worse, a nav item from an application you were not running.

Give each application its own folder. That is the entire mitigation, and it is why the folder is the unit.

The same applies to a stray `.play` file left beneath the root: a scratch file, a half-finished experiment, a
copy saved "just in case". If it is beneath the folder, it is part of the application.

## See also

- [URLs of a running Stage](urls.md) — what the session exposes once the folder has compiled.
- [Screenplay folders](/screenplay/folders/) — the language-level authority on the merge, declaration by declaration.
