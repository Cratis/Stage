---
title: How the Stage renders a model
description: How the running Stage draws a model's screens, screen templates and layouts through the default blueprint.
---

The Stage draws the model it was started with. It does not generate screens for it and it does not own a shell of
its own: it renders the scene the model describes through the default blueprint
(`@cratis/scene.blueprint.default`), the same application shell, chrome, themes and components a Scene
application gets.

## What is drawn

| Part of the model | Rendered as |
| --- | --- |
| `screen` | One entry in the blueprint menu, addressed by `#/<ScreenName>`. |
| `screen template` | The template's `arrangement flow` positions the screen's slots, evaluated for the current size class. |
| `layout` | The blueprint's shell for that layout name (`AppShell`, `FullPage`). A layout the blueprint does not provide is drawn in `AppShell`. |
| `ui profile` | Decides which packages a bare component name resolves against. `core`, `PrimeReact` and the blueprint are always admitted. |
| Command and query | Components wired to the routes the running application exposes. |

A bare component name in a template (for example `title` or `inputText`) is resolved against the profile's
packages before it is rendered, so a template written against the blueprint's vocabulary draws real components
rather than placeholders. A name no package declares renders a placeholder that names what is missing.

## Chrome

The topbar, sidebar, menu, breadcrumb, footer and settings panel are the blueprint's own. The Stage fills them:
the menu lists the model's screens, the breadcrumb shows the current one, and the topbar carries the blueprint's
theme switcher and, when the model declares more than one locale, a locale picker.

Themes are the blueprint's (`Scene Default Light` and `Scene Default Dark`). The active theme's colour scheme is
mirrored onto the document root so PrimeReact overlays, which are portalled to `body`, follow it.

## When the model declares no screens

A model with no `screen` still renders. The Stage synthesizes one screen per slice that carries a command or a
read model and places it in the same shell. That is a fallback for a model that has not designed its screens yet;
as soon as the model declares its own screens they are what is drawn.
