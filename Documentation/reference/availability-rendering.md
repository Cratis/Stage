---
title: Availability rendering
description: How Stage renders the audiences, availability blocks and available gates of a Screenplay model, how the gates are evaluated and where the audiences come from, and what Stage refuses.
---

`policy` and `authorize` say who may act. Availability says whether a part of the application is there at all for the caller's tenant, user or system. Stage renders a model's availability as a generated registration that Arc evaluates before `authorize`, through the Choreography availability engine.

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
| `availability` | A reusable block in the Choreography `AvailabilityCatalog`. The `for`, `when` and `with` lines of a block all have to hold. |
| `available` on a module, feature or slice | A gate on the namespace of the part, so it covers everything below it. |
| `available` on a command | A gate on the generated command. |
| `available` on a query | A gate on the read model that declares the query. |
| `available` on a screen | An exact-name marker part, including the module, feature and slice gates above it. The client hides its navigation entry and refuses its route. |
| `$context` values | The same catalog of provided values that `provide` renders, so a condition reads the value where it is declared. |

Gates combine down the path, outermost first. A closed gate refuses the request with the `NotAvailable` reason before authorization and validation, as Arc's availability hook does, and Arc exposes what is unavailable at `/.cratis/availability` so clients can hide navigation. Unknown never opens a gate: a missing audience, a missing value or a failing source leaves the part unavailable.

A reaction that `invokes` a command runs as the system with the tenant it works for, so its audiences are the tenant's plus the system's.

Events, projections and reactions are never gated.

## Screen navigation and routes

Gated screens also get `GeneratedAvailability/ScreenParts.cs`. Each marker has a stable name derived from its semantic identity, an empty Arc slice location, and an explicit context scope. Closing the marker therefore does not hide unrelated command or query proxies. The generated `src/stage.ts` and live `/stage/routes` publish a `screens` map from Scene screen names to exact marker keys. Ungated screens are absent from that map. The availability surface manifest identifies each screen by its semantic path with `slices=0`: a screen owns no backend slices.

The Stage frontend requests `/.cratis/availability?waitForFirstResult=true` with same-origin credentials. Gated navigation, including the fallback menu when no navigation regions are declared, is hidden while the first decision is pending, and stays hidden when the part is unavailable or the request fails or returns malformed data. Navigation regions, fallback menus, inherited contributions, breadcrumbs and opening actions all use one availability-filtered view: both a contribution's owning screen and its destination must be available. The projection walks authored values rather than enumerating navigation property names, so new component properties cannot retain a denied screen id. A denied reference removes its enclosing offering; arrays retain other offerings. Authored strings equal to a denied screen id are conservatively treated as references, including literal values. Internal links and named screen/dialog owners are checked too. The same decision refuses deep links, dialogs and routed slots with a not-found view before protected content mounts. Ungated screens stay visible without an availability request. The snapshot refreshes on window focus and every 30 seconds; an available screen keeps its mounted content and form state while a refresh is pending or returns the same decision. An unavailable decision or failed refresh revokes access. Stage waits for the routes lookup before mounting the application, so screen content cannot flash before its gate map arrives. A failed routes lookup hides all screen entries and refuses screen routes because their gates are unknown. A successful legacy response without a `screens` map still treats screens as ungated.

This is a client-only convenience guard, not a security boundary. Server-side enforcement remains on the screen's commands and queries, with their own availability and authorization rules. Never rely on hiding a screen to protect an endpoint. The backend must register the generated marker gates: Arc 23.5 returns only unavailable parts (`data.unavailable`, keyed by `name`), so a successful empty list means available. It is not a roster of registered parts. The live routes map alone does not install backend gates.

Stage currently owns this small fetch bridge because Showrunner Notes' Arc 22 peer dependencies prevent importing Arc 23's availability components. Package pins are unchanged. Replace the bridge with Arc's availability provider and guard once Showrunner supports Arc 23.

## Where the audiences come from

The generated application registers the Showrunner GuestList as the engine's audience source. The product is named after the application in lower case. GuestList records the grants and withdrawals the operator decides at the tenant, user and system level, and the gates read what it recorded.

`Availability:GrantAllDeclaredAudiences` in configuration grants every declared audience to every request. The engine allows it only in a Development, Local, Test or Testing environment and refuses to start otherwise.

## What Stage refuses

| Code | Refused |
| --- | --- |
| `STAGE-ESM-077` | A condition the engine cannot evaluate: a comparison with another `$context` value, or with a value that is not text, a number or a Boolean. |
| `STAGE-ESM-078` | Queries of one read model that carry different gates. Arc gates the read model as a whole, so give every query of it the same gate. |

Screenplay itself refuses `contains` and `starts with` in an availability condition.

Provided values and gates are resolved by the generated application at request time through Arc 23. The scaffold keeps the frontend packages on the Arc release it was frozen with.
