---
title: Projects — what a folder means to Stage
description: How Stage turns a folder of .play files into one application — discovery, merge order, what must be declared only once, and the one hazard to know about.
---

Point Stage at a folder and it runs **one application**. Not one per file, and not a set of applications that
happen to share a directory — the folder *is* the unit.

```bash
stage ./eventmodel          # the folder, as one application
stage ./eventmodel/app.play # one file, on its own
```

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

Singular across the whole folder: `domain`, `layout`, `theme` and `ui profile`. Within a `ui profile`,
`blueprint` and `start screen` are each at most one.

Everything else merges: modules combine, features and slices join their module, imports are merged and
de-duplicated, and contributions from anywhere in the tree aggregate into the point they target.

## The one hazard

**Two unrelated applications in one folder will be merged**, because nothing in the folder says they are
separate. The symptom is usually a duplicate-declaration error naming two files you did not think were related —
or worse, a nav item from an application you were not running.

Give each application its own folder. That is the entire mitigation, and it is why the folder is the unit.

The same applies to a stray `.play` file left beneath the root: a scratch file, a half-finished experiment, a
copy saved "just in case". If it is beneath the folder, it is part of the application.

## See also

- [URLs of a running Stage](urls.md) — what the session exposes once the folder has compiled.
- Screenplay's `folders.md` — the language-level authority on the merge, declaration by declaration.
