---
title: Provided values rendering
description: How Stage renders the values a Screenplay model adds to its request context with provide, which sources and types render, and what Stage refuses.
---

A model adds a value to `$context` with `provide <name> <Type>[?] from <source>` in the body of the application, a module, a feature or a slice. Stage renders the declarations into the generated application so the values resolve lazily, once per request, through the Choreography context value engine.

```text
module Fleet
  provide region String from "Nordics"
  provide operator String optional from host

  feature Vessels
    slice StateView Vessels
      provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

      query VesselByKey => Vessel optional
        by vesselId Uuid
```

## What Stage generates

When the model provides a value, Stage adds `GeneratedProvides/ProvidedValues.cs` to the backend project. It holds a `ProvidedValueResolvers` service that builds a Choreography `ContextValueCatalog` with one declaration for each `provide`, at the module, feature or slice that declares it. `ProvidedValueResolvers.Create(scope, ambient)` returns the `ContextValueResolver` of one request: `scope` names the module, feature and slice the request runs in, and `ambient` supplies the request's own values such as `command` or `arguments`.

The generated host registers `ProvidedValueResolvers` as a scoped service and an `IHostContextValues` that starts empty. Replace it from `Customizations/Program.cs` to supply the values a `from host` source reads. The nearest declaration wins, a value declared at a slice is invisible to a sibling slice, and a value declared without `?` is required, so reading it while absent throws `RequiredContextValueAbsentException`.

| Source | Generated provider |
| --- | --- |
| A literal | `LiteralContextValueProvider` |
| A `$context` expression | `ExpressionContextValueProvider` |
| `host` | `HostContextValueProvider` over `IHostContextValues` |
| `query <Query> by <$context value>` | `ReadModelContextValueProvider` that calls the generated query |

## What Stage refuses

A render that cannot honor a provided value fails with a diagnostic instead of resolving it differently.

| Code | Refused |
| --- | --- |
| `STAGE-ESM-075` | A query source that is not a keyed snapshot query by a Uuid or String key, that has filters, composite keys, a performer, authorization or narrowing, that is keyed by anything but one `$context` value, that is declared apart from its read model, or that belongs to a slice the render does not select. An in-process read would bypass the query's authorization, so a protected query is refused. |
| `STAGE-ESM-076` | A collection, a concept or a composite type. A provided value is a read model, a Uuid, a String, a number or a Boolean. |
| `STAGE-ESM-016` | An `audience`, an `availability` or an `available` gate, which Stage does not render yet. |

Code and `file` sources are not part of ESM 53: Screenplay refuses them before Stage sees the model.

## Reading a provided value in a command handler

A command handler body can read only its `Command` snapshot. A model that provides values still renders when its handlers ignore them, and a handler that reads one as `context.<Name>` is refused with `STAGE-ESM-015`.
