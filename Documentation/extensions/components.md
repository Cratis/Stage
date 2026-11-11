---
title: Run extension components
description: How Cratis Stage serves the extension components placed on a screen - public settings only, invoking an ability from the browser through the contract's allowlist and the screen's authorization, and static placeholders - and what is not yet in the browser runtime.
---

An extension can bring frontend components as well as a backend. A screen places one with `component <System>.<Component>`, and Stage serves it
the things the
[extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md) promises it. This page covers the
server side of that: what reaches the browser, and what a component may ask Stage to do. To build a component, see
[Build an extension component](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/build-a-component.md).

> [!NOTE]
> The loading of a component's frontend into the Stage browser runtime, and the supply of the rest of the context (user, tenant, locale, theme,
> strings, `raise` and `on`), depend on the `@cratis/scene.extensions` package, which is not published to npm yet. This page describes only what
> Stage does today.

## Public settings only

A system's setting is `public` or `secret`, never both. The component's context carries the public settings and nothing else:

- A secret is not read, not named and not listed. It reaches a backend only through `GetSecret` (see [Host services](host-services.md)).
- A value nobody declared is never sent.
- Settings are those of the component's own system.

```http
GET /stage/extension-components/Orders/Slack/ChannelPicker/settings
```

```json
{ "settings": { "workspace": "acme" } }
```

The route is `/stage/extension-components/<Screen>/<System>/<Component>`. It answers `404` when the screen does not place that component.

## Invoking an ability from the browser

```http
POST /stage/extension-components/Orders/Slack/ChannelPicker/invoke
Content-Type: application/json

{ "ability": "Send", "interaction": "click-1", "input": "{\"channel\":\"ops\"}" }
```

Stage checks the request in this order and stops at the first refusal:

| Check | Refusal | Status |
| --- | --- | --- |
| The screen places the component | `NotPlaced` | `404` |
| The contract's `invokes` allowlist names the ability | `NotAllowed` | `403` |
| The person is authenticated and satisfies the screen's policy | `Forbidden` | `401` or `403` |
| The ability path admits the invocation | `Refused` | `400` |

Only then is the invocation recorded and run on the same path a reaction or a workflow uses, with the origin kind `ScreenInteraction`. The
request waits for the outcome (30 seconds, set with `BrowserInvocationOptions`) and answers `200` with the `outcome` and `output` when it settles,
or `202` with the invocation `identity` when it does not.

The `interaction` is chosen by the browser and is the invocation identity of that interaction. Asking again with the same one, for a retry or a
double click, finds the invocation that exists and does not do the effect twice. The person is part of the cause, so one person's interaction can
never find another's.

## Static placeholders

A page without a browser runtime (a preview, an export, the first paint) cannot run a component. `ExtensionComponentPlaceholder` renders an element
that names the component and how it is hosted, `aria-busy`, and nothing from the context:

```html
<div data-stage-extension-component="Slack.ChannelPicker" data-kind="mount" role="region" aria-label="Slack.ChannelPicker" aria-busy="true"></div>
```

## See also

- [Extension backends](index.md) and the [host services reference](host-services.md)
- [Extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md) and
  [building a component](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/build-a-component.md) in Scene
- [Screens](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/screens.md) in Screenplay, where a screen places a component
- [Custom packages](https://github.com/Cratis/Stage/blob/main/Documentation/reference/custom-packages.md)
