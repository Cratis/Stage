---
title: Availability rendering
description: "How Stage renders the audiences, availability blocks and available gates of a model, where audiences come from, how gates evaluate and what is refused."
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
| `$context` values | The same catalog of provided values that `provide` renders, so a condition reads the value where it is declared. |

Gates combine down the path, outermost first. A closed gate refuses the request with the `NotAvailable` reason before authorization and validation, as Arc's availability hook does, and Arc exposes what is unavailable at `/.cratis/availability` so clients can hide navigation. Unknown never opens a gate: a missing audience, a missing value or a failing source leaves the part unavailable.

A reaction that `invokes` a command runs as the system with the tenant it works for, so its audiences are the tenant's plus the system's.

Events, projections and reactions are never gated.

## Where the audiences come from

The generated application registers the Showrunner GuestList as the engine's audience source. The product is named after the application in lower case. GuestList records the grants and withdrawals the operator decides at the tenant, user and system level, and the gates read what it recorded.

`Availability:GrantAllDeclaredAudiences` in configuration grants every declared audience to every request. The engine allows it only in a Development, Local, Test or Testing environment and refuses to start otherwise.

## What Stage refuses

| Code | Refused |
| --- | --- |
| `STAGE-ESM-077` | A condition the engine cannot evaluate: a comparison with another `$context` value, or with a value that is not text, a number or a Boolean. |
| `STAGE-ESM-078` | Queries of one read model that carry different gates. Arc gates the read model as a whole, so give every query of it the same gate. |
| `STAGE-ESM-016` | An `available` gate on a screen, which has no backend part to gate. |

Screenplay itself refuses `contains` and `starts with` in an availability condition.

Provided values and gates are resolved by the generated application at request time through Arc 23. The scaffold keeps the frontend packages on the Arc release it was frozen with.
