---
title: Run extension components
description: How Cratis Stage runs the extension components placed on a screen - loading React and mount components over the Scene contract, the context they receive, public settings only, invoking an ability from the browser through the contract allowlist and the screen authorization, and static placeholders.
---

An extension can bring frontend components as well as a backend. A screen places one with `component <System>.<Component>`, and Stage serves it
the things the
[extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md) promises it. This page covers the
server side of that: what reaches the browser, and what a component may ask Stage to do. To build a component, see
[Build an extension component](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/build-a-component.md).

Where the catalog of placements comes from, and how a model reaches a running extension, is on
[From a model to a running extension](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/end-to-end.md).

The browser half runs in the Stage runtime on `@cratis/scene.extensions` 5.3.0. A screen's component is a `Stage:extensionComponent` element, and the
runtime loads its frontend, builds the contract's context and renders or mounts it. See [In the browser](#in-the-browser).

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

## In the browser

The Stage runtime places a component from an element with these properties:

| Property | Meaning |
| --- | --- |
| `system`, `component` | The system and the component's name in its contract. The module must export the component under this name. |
| `screen` | The screen that places it. The screen being shown when absent. |
| `kind` | `react` or `mount`, as the model says. The module's definition must be the same kind, or the placeholder says which of the two is wrong. |
| `module` | The address of the extension's frontend module, served from the locked package (`extension-assets/{id}/…`, see [Packages](packages.md)). |
| `properties` | The properties the component is given. |
| `invokes`, `signals` | The abilities it may invoke and the signals it may raise, from the contract. |
| `strings` | The strings of the screen, by key. |
| `placeholder` | The address of the component's placeholder, shown in a frame that cannot run script until the component is ready. |

**React components** are rendered in Stage's own React tree, inside Scene's `ExtensionContextProvider`, so the `useUser`, `useTenant`, `useLocale`,
`useTheme`, `useSettings`, `useStrings`, `useInvoke`, `useRaise` and `useTrigger` hooks work. They share React with Stage: `react`, `react-dom` and
`@cratis/scene.extensions` are on the import map of the page, so an extension that imports them by name gets Stage's copy (see
[Custom packages](https://github.com/Cratis/Stage/blob/main/Documentation/reference/custom-packages.md)). **Mount components** (Vue, Svelte, Lit, web
components, plain DOM) are given a host element, their properties and the context, and are updated and unmounted with the screen.

The context is made of:

| Part | From |
| --- | --- |
| `user` | Arc's identity endpoint (`.cratis/me`): who the person is and the roles they hold. |
| `tenant` | The tenant the identity names, or `default`. |
| `locale` | The language the Stage shows and its available languages, and the browser's time zone. Changing the language updates the context. |
| `theme` | Whether the page is light or dark, and the `--cratis-*` design tokens in force. A change of theme updates the context. |
| `settings` | The public settings of the component's system, read from the settings endpoint above. |
| `strings` | The strings of the element. |
| `invoke` | The invoke endpoint above, on one interaction identity kept across the waits, so asking again never invokes twice. An ability outside `invokes` is refused in the browser as well as by the host, and a refusal arrives as an `ExtensionInvocationError` with the status and the reason the host gave. |
| `raise` | A signal in `signals`. It reaches the `onSignal` of `StageExtensionServicesProvider` and is dispatched as a bubbling `stage-extension-signal` event on the component's element. |
| `on` | A trigger sent to the component's element as a `stage-extension-trigger` event, with `detail` `{ name, values }`. |

**Secrets never reach the browser.** The only requests a component's page makes are the identity, the public settings and invocations, and an
invocation carries the ability, the interaction identity and the inputs the component gave, and nothing else. The credentials and the secret
settings stay on the host, which runs the ability.

Two things are not final. Scene 5.3.0 has no trigger kind for a signal, so a screen's `on <signal>` interactions are not yet bound to a raised
signal by Scene: the Stage delivers the signal as above, and the binding follows when Scene adds the trigger. Scene 5.3.1, which only changes
`@cratis/scene.model`, is not used because the publish of `@cratis/scene.extensions` 5.3.1 is not finished; Stage depends on 5.3.0 of every Scene package.

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
