# Samples

Screenplay applications and product-owned customization examples.

For the Screenplay samples, each folder is one application — every `.play` file inside it describes part of the
same document, and names resolve across all of them. That is not a convention this folder invented; it is what
`PlayFileCompiler.CompileFolder` means.

| Sample | What it is |
| --- | --- |
| [`Billing`](Billing) | An invoicing application, and the broadest exercise of the language in this repository. |
| [`Customization`](Customization) | C# and CSS fragments to copy into a rendered application, not a standalone Screenplay application. |

## Customization

Copy `Customization/Customizations/` into a rendered application's root. The sample registers an HTTP catalog
adapter, adds `/api/catalog/products/{id}`, and overrides product tokens without editing managed files. Set
`ProductCatalog__BaseAddress` to your catalog's base URL; the adapter expects `products/{id}` to return JSON with a
`name` property or HTTP 404. The fragments must be compiled in the rendered host, not run on their own.

See [Customize a rendered application](../Documentation/guides/customize-rendered-application.md) for the hook
signatures, configuration, dependency imports, and ownership limits.

## Billing

Two modules, four features, eight slices, fourteen files.

```text
Billing/
  application.play                 domain, concepts, types, policies, personas, authentication, trigger
  ui.play                          themes, the application shell, and one ui profile per deployment target
  behaviors.play                   named behaviors, attached elsewhere with `uses`
  seed.play                        events a fresh environment starts from
  Invoicing/
    Invoicing.play                 the module: templates, forms, navigation contributions
    Invoices/
      Invoices.play                the feature
      RegisterInvoice/             StateChange - command, event, constraint, specifications, screen
      IssueInvoice/                StateChange - reads state, requirements, state projection
      CancelInvoice/               StateChange - authorization, context mapping
      InvoiceList/                 StateView - readmodel, projection, queries, two screens
    Reminders/
      Reminders.play               Automation - scheduled and event-driven reactions
  Ledger/
    Ledger.play                    a second module, so cross-module navigation is real
    Payments/                      inline C# - a handler, a performer, a reducer rule, a validation block
    Integration/                   Translate - a capture (CDL) over a legacy system
```

The split is deliberate. A slice is a folder, so the events, the command, the read model and the screen that
belong to one decision sit together — which is the unit people actually change. `Invoicing` is the domain the
application is about; `Ledger` is what other systems talk to, and it exists so `Invoicing` never has to know
that a legacy system is still the system of record for payments.

### What it covers

**112 of the language's constructs**, verified by a coverage check rather than claimed: every slice type,
both sub-languages (PDL projections and CDL captures), both arrangement kinds, all ten interaction actions
reachable from a document, inline C# in each of the four places it is allowed, and the three forms a
localizable message can take.

Two constructs are deliberately absent: a `file` directive, because it would point at implementation files
this repository does not carry, and inline `typescript`/`react`/`html`/`sql` blocks, because `csharp` already
demonstrates the mechanism and four more would be repetition rather than coverage.

### Running it

```bash
# Compile and report - anything wrong with the document, with a location
screenplay Samples/Billing

# Serve it: the modeled backend, and the frontend translated to Scene
docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 -v "$PWD/Samples/Billing":/eventmodel:ro cratis/stage:latest
```

### It is kept honest by the build

`Source/Contracts/Scene/for_Samples/when_translating_the_billing_sample.cs` compiles this folder on every test
run and asserts **zero errors, zero warnings, and zero translation findings**.

That spec exists because this sample has already found two real defects that every unit spec missed:

- `target size expanded` — valid Screenplay, used in Screenplay's own documentation — **crashed** the
  translation, because Stage parsed a target's assumed size against the two-axis arrangement matrix, which has
  no such value.
- Behaviors written inside a filled template slot were **dropped without a word**, because the conversion only
  looked at a screen's top-level directives. Nobody writes an interaction at the top of a templated screen;
  they write it next to the thing it acts on, which is exactly what no unit spec did.

Both are fixed and pinned by their own specs. A broad, realistic document finds this class of defect and a
focused one does not, which is the argument for keeping this sample real rather than minimal.
