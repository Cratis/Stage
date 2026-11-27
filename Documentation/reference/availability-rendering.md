---
title: Availability rendering
description: "How Stage renders the audiences, availability blocks and available gates of a model, where audiences come from, how gates evaluate and what is refused."
---

`policy` and `authorize` say who may act. Availability says whether a part of the application is there at all for the caller's tenant, user or system. Stage renders a model's availability as a generated registration that Arc evaluates before `authorize`, through Arc's availability engine.

```text
audience Plus
  description "Customers on the Plus package"

availability DataQuality
  description "Sold with Plus once the region is known"
  for Plus
  when $context.region == "Nordics"

module Fleet
  provide region String from "Nordics"

  feature Vessels
    available with DataQuality

    slice StateChange RegisterVessel
      available for Plus or Beta
```

## What Stage generates

A model that uses availability gets `GeneratedAvailability/Availability.cs` and `GeneratedAvailability/Availability.props`. A model that does not gets neither, and the scaffold's `GeneratedAvailabilityRegistration.cs` does nothing.

| Model | Generated |
| --- | --- |
| `audience` | The declared audiences of the application. A grant for an audience the model does not declare never opens a gate. |
| `availability` | A reusable block in Arc's `AvailabilityCatalog`. The `for`, `when` and `with` lines of a block all have to hold. |
| `available` on a module, feature or slice | A gate on the namespace of the part, so it covers everything below it. |
| `available` on a command | A gate on the generated command. |
| `available` on a query | A gate on the read model that declares the query. |
| `available` on a screen | An exact-name marker part, including the module, feature and slice gates above it. The client hides its navigation entry and refuses its route. |
| `available` on a screen action or data element (ESM v59) | An exact-name marker part of its own, with every gate above it (module, feature, slice and screen) in front of the element's own. The client asks about the marker with the values the screen shows. |
| `available not for Audience` (ESM v58) | Arc's `AvailableNotAudience`: the part is open for everyone except the tenants, users or systems that hold the audience. |
| `$context` values | The same catalog of provided values that `provide` renders, so a condition reads the value where it is declared. |

Gates combine down the path, outermost first. A closed gate refuses the request with the `NotAvailable` reason before authorization and validation, as Arc's availability hook does, and Arc exposes what is unavailable at `/.cratis/availability` so clients can hide navigation. Unknown never opens a gate: a missing audience, a missing value or a failing source leaves the part unavailable.

A reaction that `invokes` a command runs as the system with the tenant it works for, so its audiences are the tenant's plus the system's.

Events, projections and reactions are never gated.

## Screen navigation and routes

Gated screens also get `GeneratedAvailability/ScreenParts.cs`. Each marker has a stable name derived from its semantic identity, an empty Arc slice location, and an explicit context scope. Closing the marker therefore does not hide unrelated command or query proxies. The generated `src/stage.ts` and live `/stage/routes` publish a `screens` map from Scene screen names to exact marker keys. Ungated screens are absent from that map. The availability surface manifest identifies each screen by its semantic path with `slices=0`: a screen owns no backend slices.

The Stage frontend observes `/.cratis/availability` through Arc's availability provider, mounted only for an application with a gated screen or element. Gated navigation, including the fallback menu when no navigation regions are declared, is hidden while the first decision is pending, and stays hidden when the part is unavailable or no decision has been made. A later decision replaces the last one live, without a reload; a failed update keeps the last decision, because Arc takes a failure to decide as neither a denial nor a grant. Navigation regions, fallback menus, inherited contributions, breadcrumbs and opening actions all use one availability-filtered view: both a contribution's owning screen and its destination must be available. The projection follows explicitly classified navigation fields in the Scene model and supported component properties, including breadcrumb `homeTargetScreen`, user-menu `items`, `core:navigate.targetScreen`, action-button `targetScreen` when no Click behavior overrides it, form-response continuations, nested destinations, internal routes, opening actions and named screen/dialog owners. A denied reference removes its enclosing offering; arrays retain other offerings. Layout, template and slot identities, data ids, literal bindings and opaque component data belong to separate namespaces and are preserved even when their values equal a denied screen name. An exhaustive model-field specification requires new typed properties to be classified as navigation references or justified non-references. A consumer-source specification also checks property readers in the pinned Blueprint components and runtime (including `.ts` helpers and property-object readers such as `textProperty`) against the semantic property policies or explicit non-navigation classifications. It checks navigation-shaped keys and local values passed to screen-navigation calls or events. New navigation-capable component properties must be registered in the semantic property policy. The same decision refuses deep links, dialogs and routed slots with a not-found view before protected content mounts. Ungated screens stay visible without an availability request. The snapshot refreshes on window focus and every 30 seconds; an available screen keeps its mounted content and form state while a refresh is pending or returns the same decision. An unavailable decision or failed refresh revokes access. Stage waits for the routes lookup before mounting the application, so screen content cannot flash before its gate map arrives. A failed routes lookup hides all screen entries and refuses screen routes because their gates are unknown. A successful legacy response without a `screens` map still treats screens as ungated.

This is a client-only convenience guard, not a security boundary. Server-side enforcement remains on the screen's commands and queries, with their own availability and authorization rules. Never rely on hiding a screen to protect an endpoint. The backend must register the generated marker gates: Arc 23.5 returns only unavailable parts (`data.unavailable`, keyed by `name`), so a successful empty list means available. It is not a roster of registered parts. The live routes map alone does not install backend gates. The live Stage Host currently publishes these keys without registering the marker types or their availability rules, so live-host screen availability is not supported end to end. Use the rendered application with its generated availability registration for evaluated screen gates. Live query-backed providers still require a tenant data boundary and live-update design; they are not part of this screen-guard delivery.

The Stage frontend uses Arc's availability client (`AvailabilityProvider`, `useAvailability` and `useIsAvailable` from `@cratis/arc.react/availability`) since it moved to Arc 24. The client reads the reason of every refusal, so a part an operator suspended (`Suspended`) is told apart from one that is not in the caller's offer (`NotAvailable`), and it can ask about one part and the data it is about (`part` and `arguments`).

A screen an operator suspended answers its route with a short "Back soon" message instead of "Screen not found". Its navigation entry stays hidden either way.

## Screen elements

An action or a data element of a screen can carry its own `available` gate. The renderer follows these rules.

| Element | What the client does |
| --- | --- |
| An action or form for a command that is not available | The element is not rendered. Arc says so with the `NotAvailable` reason on the command's own check, which is asked with the data the screen shows. |
| An action or form for a command an operator suspended | A short message, "Back soon", takes the element's place until the suspension is lifted. |
| An action for a command the caller may not use | The renderer's rule for unauthorized: the action stays visible, disabled, with the reason the server gave. |
| A data element whose read model is not available | The element is not rendered. The query answers the refusal and the client reads its reason. |
| A data element whose read model an operator suspended | The same short message. |
| An element with its own gate | The marker is asked about at `/.cratis/availability` with `part` and `arguments`, through `useIsAvailable`, before anything is read or offered. Nothing is shown while the answer is pending, and a failed answer keeps the element closed. |

What withholds an element, strongest first: a part that is not available, a part an operator suspended, an answer still pending, a caller who may not use the command. The generated `src/stage.ts` publishes the markers as `routes.elements`, keyed `Screen/action/Command` and `Screen/data/Query`.

One marker carries one gate, so the same command or query placed more than once on a screen with different gates is refused with `STAGE-ESM-079`. Give every placement the same gate.

## Where the audiences come from

The generated application registers the Showrunner GuestList as the engine's audience source. The product is named after the application in lower case. GuestList records the grants and withdrawals the operator decides at the tenant, user and system level, and the gates read what it recorded.

`Availability:GrantAllDeclaredAudiences` in configuration grants every declared audience to every request. The engine allows it only in a Development, Local, Test or Testing environment and refuses to start otherwise.

## What Stage refuses

| Code | Refused |
| --- | --- |
| `STAGE-ESM-077` | A condition the engine cannot evaluate: a comparison with another `$context` value, or with a value that is not text, a number or a Boolean. |
| `STAGE-ESM-078` | Queries of one read model that carry different gates. Arc gates the read model as a whole, so give every query of it the same gate. |

Screenplay itself refuses `contains` and `starts with` in an availability condition.

Provided values and gates are resolved by the generated application at request time through Arc's availability engine (Arc 24.8). The scaffold keeps the frontend packages on the Arc release it was frozen with.
