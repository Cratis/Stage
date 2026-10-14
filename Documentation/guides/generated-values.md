---
title: Generate identifiers and values from a model
description: Follow a small shop from a Screenplay model to rendered C#, then see when values are generated, how fresh they are, and when not to generate at all.
---

A create command wants to hand back the identifier of the thing it just made. If the caller invents that identifier, you inherit its collisions, its guessable shapes and its typos. If you invent it in the handler, you reach for `Guid.NewGuid()` and lose the shape, the ordering and the testability you wanted.

Screenplay splits the decision in two. A **concept** owns the strategy: `generate ulid`, `generate typeid "order"`, `generate nanoid`. A **command property** only says `generated`. Stage renders that into Arc and Chronicle code, and this guide follows one small shop through the whole path:

| Value | Concept | What it is | Strategy |
| --- | --- | --- | --- |
| `CustomerId` | `cus_` + ULID | Event source of a customer | `ulid` with a prefix |
| `OrderId` | `order_` + TypeID suffix | Event source of an order | `typeid "order"` |
| `InviteCode` | A short opaque code | A value inside an event | `nanoid` |
| `ApiKeyId` | A UUID | Event source of an API key | Implicit `uuid` |
| `ApiKey` | A secret | A protected value inside an event | None. A caller supplies it |

By the end you have rendered the shop with Stage, read the C# it produced, and seen its 21 generated specifications pass. The last two sections tell you which strategy to pick and when to skip generation.

## Declare the strategy on the concept

Start with the convention path: a concept that says how its values are made, and a command whose generated property is the identifier. This is an excerpt of the shop model; the other slices appear where they matter.

```screenplay title="shop.play (excerpt)"
concept CustomerId : String
  generate ulid prefix "cus_" case lower
concept OrderId : String
  generate typeid "order"
concept InviteCode : String
  generate nanoid
concept ApiKeyId : Uuid
concept ApiKey : String secret
concept CustomerName : String
module Storefront
  feature Customers
    slice StateChange Register
      command RegisterCustomer
        customerId CustomerId generated identifier
        name CustomerName
        produces event CustomerRegistered
          name CustomerName = name
          registeredAt DateTime = $context.occurred
        returns customerId
```

Three things carry the design:

- `generate ulid prefix "cus_" case lower` lives on `CustomerId`, not on the command. Every command that generates a `CustomerId` shares the strategy.
- `customerId CustomerId generated identifier` removes the property from the request. The caller sends a name and receives an identifier.
- `returns customerId` carries the generated value back as the command's response.

`registeredAt DateTime = $context.occurred` maps the command's receipt time into the event. It matters later, when time enters the picture.

Screenplay checks the strategy while compiling. A generated identifier needs at least 64 random bits, an unsupported strategy is refused, and the model's semantic version rises to ESM v15 as soon as one concept serializes a strategy. The [Screenplay generated values page](/screenplay/generated-values/) owns the syntax and the compiler's rules.

## Render it with Stage

Stage plans artifacts from `.play` files without writing anything. The console below is the program that produced every output on this page. It references the `Rendering.Cratis` project from this repository's `Source/` folder and calls `CratisRendering.PlanFrom`, described in [Plan artifacts from Screenplay sources](../reference/plan-from-sources.md).

```csharp title="Program.cs"
using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis;

var modelRoot = Path.GetFullPath(args[0]);
var outputRoot = Path.GetFullPath(args[1]);

var options = new CratisPlanOptions("Shop", "Shop", "Acme.Shop");
var scaffold = CratisRendering.PlanScaffold(options);
var result = await CratisRendering.PlanFrom(
    new PlaySources(modelRoot, ImmutableArray<string>.Empty),
    new PlanSelection([PlanSelectionEntry.Module("Storefront")]),
    options);

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message} [{diagnostic.Source}]");
}

Console.WriteLine($"Success={result.Success} artifacts={result.Artifacts.Length} scaffold={scaffold.Artifacts.Length}");
if (!result.Success)
{
    return 1;
}

// Let Stage compare the plan with what is already on disk, and never overwrite on a refusal.
var publication = CratisRendering.CheckPublication(result.Plan!, relativePath =>
{
    var existing = Path.Combine(outputRoot, relativePath);
    return File.Exists(existing) ? File.ReadAllText(existing) : null;
});
Console.WriteLine($"Publication check: {publication}");
if (publication is not CratisPublicationCheck.Compatible)
{
    return 1;
}

foreach (var artifact in scaffold.Artifacts.Concat(result.Artifacts))
{
    var path = Path.Combine(outputRoot, artifact.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, [.. artifact.Bytes]);
}

return 0;
```

Run it with the folder that holds `shop.play` and a folder to write into:

```bash
dotnet render/bin/Debug/net10.0/DocsGvRender.dll shop out
```

```text title="Output"
Success=True artifacts=17 scaffold=40
Publication check: Compatible { }
```

`Success=True` means no diagnostic has error severity. The publication check compares the plan with the files already in the output folder and refuses changes that would break them, such as an event type id change (`STAGE-EVENT-002`). The program writes nothing unless the result is `Compatible`, which it is for an empty folder. A real publisher also has to protect hand-edited files; see [Build a renderer target](build-renderer-target.md). The rendered project pins Arc 22.95.0 and Chronicle 19.59.6. The slice artifacts land in these files, next to the 40 scaffold files:

```text title="Planned slice artifacts"
Common/ApiKey.cs
Common/ApiKeyId.cs
Common/CustomerId.cs
Common/CustomerName.cs
Common/InviteCode.cs
Common/OrderId.cs
GeneratedCommands/CommandReceiptTime.cs
Storefront/Customers/Invite/Invite.cs
Storefront/Customers/Invite/when_issuing_with_pinned_code.cs
Storefront/Customers/Keys/Keys.cs
Storefront/Customers/Keys/when_registering_with_pinned_identity.cs
Storefront/Customers/Register/Register.cs
Storefront/Customers/Register/when_refusing_used_identity.cs
Storefront/Customers/Register/when_registering_at_the_receipt_time.cs
Storefront/Customers/Register/when_registering_with_pinned_identity.cs
Storefront/Orders/Place/Place.cs
Storefront/Orders/Place/when_placing_at_the_receipt_time.cs
```

:::note[Rendering is not compiling]
A plan proves that Stage admitted the model. This guide also built the rendered application in Debug and ran its specifications: `dotnet build` succeeded and `dotnet test` reported 21 passed, 0 failed. Do the same for your own model before you trust it.
:::

## Read the rendered concepts

Each concept becomes a record that knows how to make itself. `CustomerId` is used as an `identifier`, so it derives from Chronicle's `EventSourceId<string>`. It is time-ordered, so it implements `IGeneratableInContext<T>`:

```csharp title="Common/CustomerId.cs (license header omitted)"
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Acme.Shop.Common;

/// <summary>
/// Represents customer id.
/// </summary>
public record CustomerId(string Value) : global::Cratis.Chronicle.Events.EventSourceId<string>(Value), global::Cratis.Concepts.IGeneratableInContext<CustomerId>
{
    public static readonly CustomerId NotSet = new(string.Empty);

    public static CustomerId New(global::Cratis.Concepts.GenerationContext context) => new("cus_" + global::Cratis.Concepts.GenerateValue.Ulid(context).ToLowerInvariant());

    public static implicit operator CustomerId(string value) => new(value);
}
```

`OrderId` has the same shape. Only the factory differs:

```csharp title="Common/OrderId.cs (license header omitted)"
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Acme.Shop.Common;

/// <summary>
/// Represents order id.
/// </summary>
public record OrderId(string Value) : global::Cratis.Chronicle.Events.EventSourceId<string>(Value), global::Cratis.Concepts.IGeneratableInContext<OrderId>
{
    public static readonly OrderId NotSet = new(string.Empty);

    public static OrderId New(global::Cratis.Concepts.GenerationContext context) => new(global::Cratis.Concepts.GenerateValue.TypeId("order", context));

    public static implicit operator OrderId(string value) => new(value);
}
```

`InviteCode` is neither time-ordered nor an identifier. It is a plain value concept with `IGeneratable<T>` and a parameterless `New()`:

```csharp title="Common/InviteCode.cs (license header omitted)"
using Cratis.Concepts;

namespace Acme.Shop.Common;

/// <summary>
/// Represents invite code.
/// </summary>
public record InviteCode(string Value) : global::Cratis.Concepts.ConceptAs<string>(Value), global::Cratis.Concepts.IGeneratable<InviteCode>
{
    public static readonly InviteCode NotSet = new(string.Empty);

    public static InviteCode New() => new(global::Cratis.Concepts.GenerateValue.NanoId(21, null));

    public static implicit operator string(InviteCode value) => value.Value;
    public static implicit operator InviteCode(string value) => new(value);
}
```

Time-ordered concepts have no parameterless `New()`. The compiler stops hand-written code from asking for a ULID without saying when. Context-free strategies keep `New()`.

## Read the rendered command and event

The command follows the model. Nothing about the identifier appears in the constructor:

```csharp title="Storefront/Customers/Register/Register.cs (license header omitted)"
using Acme.Shop.Common;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

namespace Acme.Shop.Storefront.Customers.Register;

[global::Cratis.Arc.Commands.ModelBound.CommandAttribute]
[global::Cratis.Arc.Authorization.AllowAnonymousAttribute]
public record RegisterCustomer(global::Acme.Shop.Common.CustomerName Name)
{
    public (global::Acme.Shop.Common.CustomerId, global::Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes) Handle([global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute("customerId")] global::Acme.Shop.Common.CustomerId customerId, global::Cratis.Arc.IOperationContextAccessor operation)
    {
        var occurred = global::Acme.Shop.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt(operation);
        return (customerId, new global::Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes([new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(customerId, new global::Acme.Shop.Storefront.Customers.Register.CustomerRegistered(Name, occurred)) { Occurred = occurred }], global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Distinct<global::Cratis.Chronicle.Events.EventSourceId>([customerId]), source => new global::System.Collections.Generic.KeyValuePair<global::Cratis.Chronicle.Events.EventSourceId, global::Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope>(source, new(global::Cratis.Chronicle.Events.EventSequenceNumber.BeforeFirst, EventSourceId: source)))));
    }
}

/// <summary>
/// The event that occurs when customer registered.
/// </summary>
[global::Cratis.Chronicle.Events.EventTypeAttribute]
public record CustomerRegistered(global::Acme.Shop.Common.CustomerName Name, global::System.DateTimeOffset RegisteredAt);
```

- `RegisterCustomer(CustomerName Name)` is the whole request. `customerId` is a `Handle` parameter marked `[GeneratedValue("customerId")]`, and the modeled name is the case-sensitive key.
- The return value is a tuple. The first element is the response Arc hands to the caller; the second is the events together with their concurrency scopes.
- Each generated event source gets one `ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: source)`. That is the freshness guard, covered below.
- `Occurred = occurred` comes from the `$context.occurred` mapping, which reads the receipt time Arc captured.

The generated TypeScript proxy carries the same contract to the browser. It sends `name` only and types the response as the identifier's string:

```typescript title="Shop/Storefront/Customers/Register/Register.ts (excerpt)"
export interface IRegisterCustomer {
    name?: string;
}

export class RegisterCustomer extends Command<IRegisterCustomer, string> implements IRegisterCustomer {
```

The proxy comes from the build, not from the plan: Arc's proxy generator writes it when you compile the rendered project.

## Follow one command through the runtime

When a client sends `RegisterCustomer`, this is what the rendered application does, in order. Each step names the code that decides it. Arc draws a fresh value on every execution of the command. Screenplay's contract is that generation follows authorization and validation, so a generated value cannot appear in either.

1. **Arc captures the receipt time.** At the dispatch boundary Arc reads the configured `TimeProvider` (UTC now) and keeps it for that dispatch.
2. **Arc resolves the generated parameter.** For a time-ordered concept it builds a `GenerationContext` from the receipt time and the current tenant and calls `New(context)`. For other concepts it calls `New()`. With no receipt time, generation fails with `MissingGenerationTime`; there is no wall-clock fallback in the rendered application.
3. **`Handle` runs** and returns the response with the events and scopes.
4. **Chronicle checks the scopes, then appends.** A `BeforeFirst` scope on a source means the source must hold no events. Chronicle checks every scope in a batch once, against the state before the batch, so a command may append several first facts to one fresh source. One violated scope rejects the whole batch and appends nothing.
5. **A renumbered retry is checked again.** When a storage append loses a sequence-number race, Chronicle renumbers the batch and validates the scopes again before retrying. It has done so since Chronicle 19.40.1; the pinned client is 19.59.6.
6. **Arc returns the result.** Arc uses the tuple's first element as `CommandResult<T>.Response`, so an accepted command carries the generated identifier and the TypeScript proxy resolves to the identifier string. A `returns` block would give a named response record instead; see [Responses](../reference/generated-values-rendering.md#responses). A reused source becomes a validation failure with the reason `ConcurrencyViolation`.

The generated rejection specification asserts exactly that reason and that no events were appended:

```csharp title="Storefront/Customers/Register/when_refusing_used_identity.cs (excerpt)"
async global::System.Threading.Tasks.Task Establish()
{
    _scenario.PinGeneratedValue("customerId", new global::Acme.Shop.Common.CustomerId("cus_01arz3ndektsv4rrffq69g5fav"));
    await _scenario.EventScenario.Given.ForEventSource(new global::Acme.Shop.Common.CustomerId("cus_01arz3ndektsv4rrffq69g5fav")).Events(new global::Acme.Shop.Storefront.Customers.Register.CustomerRegistered(new global::Acme.Shop.Common.CustomerName("Ada"), global::System.DateTimeOffset.Parse("2026-01-02T03:04:05.6780000Z", global::System.Globalization.CultureInfo.InvariantCulture)));
    _givenEventCount = _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess);
}

async global::System.Threading.Tasks.Task Because() => _result = await _scenario.Execute(new global::Acme.Shop.Storefront.Customers.Register.RegisterCustomer(new global::Acme.Shop.Common.CustomerName("Grace")));

[global::Xunit.FactAttribute] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[global::Xunit.FactAttribute] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
[global::Xunit.FactAttribute] void should_report_the_freshness_violation() => _result.ValidationResults.First().Reason.ShouldEqual(global::Cratis.Arc.Validation.ValidationResultReason.ConcurrencyViolation);
[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);
```

The Chronicle [concurrency page](/chronicle/events/concurrency/) explains scopes and `ConcurrencyViolation` in general.

:::caution[Freshness is not idempotency]
A rejected command is not retried with the same value. The next attempt draws a new identifier and succeeds, so a client that resends after a timeout can register two customers. When retries must be safe, let the client supply the identifier as an input and enforce uniqueness instead. See [When not to generate](#when-not-to-generate).
:::

### A generated value that is not an identity

`IssueInvite` generates an `InviteCode` but appends to the customer's stream, which the caller names:

```screenplay title="shop.play (excerpt)"
slice StateChange Invite
  command IssueInvite
    customerId CustomerId identifier
    inviteCode InviteCode generated
    produces event InviteIssued
      inviteCode InviteCode = inviteCode
    returns inviteCode
```

The rendered guard shows what Stage does with a generated value that is not the destination:

```csharp title="Storefront/Customers/Invite/Invite.cs (excerpt)"
public (global::Acme.Shop.Common.InviteCode, global::Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes) Handle([global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute("inviteCode")] global::Acme.Shop.Common.InviteCode inviteCode)
{
    return (inviteCode, new global::Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes([new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(CustomerId, new global::Acme.Shop.Storefront.Customers.Invite.InviteIssued(inviteCode))], global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Distinct<global::Cratis.Chronicle.Events.EventSourceId>([.. (global::System.String.Equals(CustomerId.Value.ToString(), inviteCode.Value, global::System.StringComparison.Ordinal) ? new global::Cratis.Chronicle.Events.EventSourceId[] { CustomerId } : [])]), source => new global::System.Collections.Generic.KeyValuePair<global::Cratis.Chronicle.Events.EventSourceId, global::Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope>(source, new(global::Cratis.Chronicle.Events.EventSequenceNumber.BeforeFirst, EventSourceId: source)))));
}
```

The scope list contains the customer id only when its text equals the generated code, a comparison that never holds for different concepts. The customer's stream keeps its existing facts, and the command makes no freshness claim about it. Nothing checks a generated non-identifier for uniqueness. A 21-character NanoID has 126 random bits, which makes a collision negligible, but a shorter code would not.

## Understand the receipt time

Time-ordered strategies take their clock from the command's receipt, and so does `$context.occurred`. Stage renders the mapping through one shared helper:

```csharp title="GeneratedCommands/CommandReceiptTime.cs (excerpt)"
internal static class CommandReceiptTime
{
    /// <summary>
    /// Gets the dispatch receipt at Chronicle's UTC-millisecond precision.
    /// </summary>
    /// <param name="operation">The active Arc operation.</param>
    /// <returns>The normalized dispatch receipt.</returns>
    /// <exception cref="CommandReceiptTimeUnavailable">No dispatch receipt is available.</exception>
    internal static global::System.DateTimeOffset OccurredAtReceipt(global::Cratis.Arc.IOperationContextAccessor operation)
    {
        var receivedAt = operation.ReceivedAt ?? throw new CommandReceiptTimeUnavailable();
        if (receivedAt == default)
        {
            throw new CommandReceiptTimeUnavailable();
        }
        return global::System.DateTimeOffset.FromUnixTimeMilliseconds(receivedAt.ToUnixTimeMilliseconds());
    }
}
```

Three consequences follow:

- The helper truncates the receipt to a whole millisecond, which is also all a ULID, a UUIDv7 or a TypeID stores. A `registeredAt` mapped from `$context.occurred` and the time prefix of `CustomerId` agree to the millisecond.
- Only a model that maps `$context.occurred` sets `Occurred` on the event. Without it, the generated `Handle` supplies no occurrence time, Arc passes none, and Chronicle stamps the time of the append, which is later than the receipt.
- Time-ordered values reveal when they were made and are not monotonic within a millisecond. Do not sort by them when you need a strict order.

The shop's order slice uses a TypeID. Only the time follows the operation: `GenerationContext` carries a tenant, but `TypeId`, `Ulid` and `UuidV7` read only its `Now`. A TypeID has no tenant slot.

```screenplay title="shop.play (excerpt)"
feature Orders
  slice StateChange Place
    command PlaceOrder
      orderId OrderId generated identifier
      customerId CustomerId
      produces event OrderPlaced
        customerId CustomerId = customerId
      returns orderId
    specification PlacingAtTheReceiptTime
      given clock "2026-01-02T03:04:05.678Z"
      when PlaceOrder
        customerId = "cus_01arz3ndektsv4rrffq69g5fav"
      then OrderPlaced
        customerId = "cus_01arz3ndektsv4rrffq69g5fav"
```

## Pin values in specifications

Random values make poor fixtures, so Screenplay never draws one. A specification either pins the value or lets Stage assert its shape. `RegisteringWithPinnedIdentity` pins the identifier with `for` and a clock with `given clock`:

```screenplay title="shop.play (excerpt)"
specification RegisteringWithPinnedIdentity
  given clock "2026-01-02T03:04:05.678Z"
  when RegisterCustomer
    for "cus_01arz3ndektsv4rrffq69g5fav"
    name = "Ada"
  then CustomerRegistered
    for "cus_01arz3ndektsv4rrffq69g5fav"
    name = "Ada"
    registeredAt = "2026-01-02T03:04:05.678Z"
  then returns "cus_01arz3ndektsv4rrffq69g5fav"
```

Stage renders the pins as scenario setup and asserts the authored values:

```csharp title="Storefront/Customers/Register/when_registering_with_pinned_identity.cs (excerpt)"
public class when_registering_with_pinned_identity : global::Cratis.Specifications.Specification, global::System.IDisposable
{
    readonly global::Cratis.Arc.Testing.Commands.CommandScenario<global::Acme.Shop.Storefront.Customers.Register.RegisterCustomer> _scenario = new();
    global::Cratis.Arc.Commands.CommandResult _result = null!;

    void Establish()
    {
        _scenario.Clock(new global::System.DateTimeOffset(639029198456780000L, global::System.TimeSpan.Zero));
        _scenario.PinGeneratedValue("customerId", new global::Acme.Shop.Common.CustomerId("cus_01arz3ndektsv4rrffq69g5fav"));
    }

    async global::System.Threading.Tasks.Task Because() => _result = await _scenario.Execute(new global::Acme.Shop.Storefront.Customers.Register.RegisterCustomer(new global::Acme.Shop.Common.CustomerName("Ada")));

    [global::Xunit.FactAttribute] void should_succeed() => _result.ShouldBeSuccessful();
    [global::Xunit.FactAttribute] async global::System.Threading.Tasks.Task should_have_appended_customer_registered() => await _scenario.ShouldHaveAppendedEvent<global::Acme.Shop.Storefront.Customers.Register.RegisterCustomer, global::Acme.Shop.Storefront.Customers.Register.CustomerRegistered>(new global::Acme.Shop.Common.CustomerId("cus_01arz3ndektsv4rrffq69g5fav"), @event => @event.Name == new global::Acme.Shop.Common.CustomerName("Ada") && @event.RegisteredAt == global::System.DateTimeOffset.Parse("2026-01-02T03:04:05.6780000Z", global::System.Globalization.CultureInfo.InvariantCulture));
    [global::Xunit.FactAttribute] void should_return_the_expected_response() => ((global::Cratis.Arc.Commands.CommandResult<global::Acme.Shop.Common.CustomerId>)_result).Response.ShouldEqual(new global::Acme.Shop.Common.CustomerId("cus_01arz3ndektsv4rrffq69g5fav"));

    public void Dispose() => _scenario.Dispose();
}
```

The pin must match the strategy's exact shape: `cus_` plus a 26-character lowercase ULID whose first character is `0`–`7`. `RegisteringAtTheReceiptTime` pins nothing. Stage sets the scenario clock and allows generation, then asserts the shape of the generated value and its time prefix:

```csharp title="Storefront/Customers/Register/when_registering_at_the_receipt_time.cs (excerpt)"
void Establish()
{
    _scenario.Clock(new global::System.DateTimeOffset(639029198456780000L, global::System.TimeSpan.Zero));
    _scenario.AllowGeneration();
}
```

The last fact checks the receipt-time prefix:

```csharp title="Storefront/Customers/Register/when_registering_at_the_receipt_time.cs (excerpt)"
[global::Xunit.FactAttribute] void should_generate_customer_id_at_the_receipt_time() => _scenario.GeneratedValues.Last<global::Acme.Shop.Common.CustomerId>("customerId").Value.ToString()!.StartsWith("cus_01kdyak3se", global::System.StringComparison.Ordinal).ShouldBeTrue();
```

`cus_01kdyak3se` is the ULID time prefix for `2026-01-02T03:04:05.678Z`. The random remainder stays unpinned.

## Keep the secret out of the identity

`ApiKey` is a `secret` concept, and the model treats it differently from everything above. The identity is a separate `ApiKeyId`, and the key itself is a value on the event:

```screenplay title="shop.play (excerpt)"
slice StateChange Keys
  command RegisterApiKey
    apiKeyId ApiKeyId generated identifier
    customerId CustomerId
    apiKey ApiKey
    produces event ApiKeyRegistered
      customerId CustomerId = customerId
      apiKey ApiKey = apiKey
    returns apiKeyId
  specification RegisteringWithPinnedIdentity
    when RegisterApiKey
      for "5b3f2a4c-9d1e-4f6a-8b7c-0d1e2f3a4b5c"
      customerId = "cus_01arz3ndektsv4rrffq69g5fav"
      apiKey = "partner-supplied-key"
    then ApiKeyRegistered
      for "5b3f2a4c-9d1e-4f6a-8b7c-0d1e2f3a4b5c"
      customerId = "cus_01arz3ndektsv4rrffq69g5fav"
      apiKey = "partner-supplied-key"
    then returns "5b3f2a4c-9d1e-4f6a-8b7c-0d1e2f3a4b5c"
```

Stage renders the compliance marking onto the concept, and the command property carries a marker of its own:

```csharp title="Common/ApiKey.cs (license header omitted)"
using Cratis.Concepts;

namespace Acme.Shop.Common;

/// <summary>
/// Represents api key.
/// </summary>
[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute]
[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]
public record ApiKey(string Value) : global::Cratis.Concepts.ConceptAs<string>(Value)
{
    public static readonly ApiKey NotSet = new(string.Empty);

    public static implicit operator string(ApiKey value) => value.Value;
    public static implicit operator ApiKey(string value) => new(value);
}
```

```csharp title="Storefront/Customers/Keys/Keys.cs (excerpt)"
[global::Cratis.Arc.Commands.ModelBound.CommandAttribute]
[global::Cratis.Arc.Authorization.AllowAnonymousAttribute]
public record RegisterApiKey(global::Acme.Shop.Common.CustomerId CustomerId, [property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] global::Acme.Shop.Common.ApiKey ApiKey)
```

The two attributes protect different things, and neither replaces the other:

| Attribute | Protects | Does not protect |
| --- | --- | --- |
| `[Encrypted]` | The value inside the stored event payload | The event-source id, the command causation chain |
| `[NotAudited]` | The command input, from the causation chain Arc writes with every event | Event payloads and logs |

`[NotAudited]` keeps a value out of the **command** causation chain, and nowhere else. A command property that holds a secret needs it; an event property that holds a secret needs `[Encrypted]`. Stage puts both on every `secret` concept, because one concept can serve as a command input and as an event value. Neither attribute makes it safe to log the value or return it.

Event-source ids are plaintext metadata and cannot be encrypted. Stage refuses compliance markers on identifiers with `STAGE-ESM-040`, which is why `ApiKeyId` is a plain UUID and `ApiKey` never becomes an event source.

### Why the shop does not generate its API key

An API key looks like a perfect `bytes 32` candidate. Declare it that way and Stage's planner refuses, relaying Screenplay's `PLAY0268`:

```screenplay title="shop-refused/refused.play"
concept ApiKeyId : Uuid
concept ApiKey : String secret
  generate bytes 32 base64url
module Storefront
  feature Keys
    slice StateChange Issue
      command IssueApiKey
        apiKeyId ApiKeyId generated identifier
        apiKey ApiKey generated
        produces event ApiKeyIssued
          apiKey ApiKey = apiKey
```

```text title="Output"
Error PLAY0268: Generated concepts with compliance markers or validation rules are not admitted in #616 phase 2b-1. [refused.play(3,3)]
Error STAGE-PLAN-003: Screenplay compilation failed. []
Success=False artifacts=0 scaffold=40
```

Screenplay does not yet admit compliance markers on a generated concept, so this model does not compile and Stage plans nothing. A generated value is a `Handle` parameter rather than a command input, so it never enters the command causation chain. Its exposure is the event payload, and only `[Encrypted]` protects that. Until generated secrets are admitted, the caller or an adapter supplies the key, as the shop model does.

## Choosing a strategy

Start from what the value must do, then pick the shape. The random-bit counts come from Screenplay's [strategy reference](/screenplay/generated-values/strategies/).

| You need | Strategy | Why |
| --- | --- | --- |
| An identity with no special shape or order | `uuid`, implicit on a `Uuid` concept | 122 random bits and no creation time. `ApiKeyId` uses it |
| A `Uuid` column with index locality | `uuidv7` | A timestamp prefix keeps inserts together. 74 random bits, and it reveals time |
| A readable, sortable string identity | `ulid`, optionally with `prefix` | 26 characters, 80 random bits. The `cus_` prefix helps people recognize it and adds no entropy |
| Identities that name what they identify across entity kinds | `typeid "order"` | The prefix reads aloud and the suffix is a UUIDv7. 74 random bits, and no tenant slot |
| A short opaque code | `nanoid` | 21 characters from a 64-symbol alphabet is 126 bits. A custom alphabet or size changes that |
| An exact random-byte budget | `bytes n base64url`, `hex` or `base32` | Entropy is exactly `8n` bits and you pick the encoding. Not admitted for secrets yet, see above |

Two checks apply to every row. A generated identifier needs at least 64 random bits, and a prefix never adds any. Shorter values on non-identifier properties produce a warning; a unique constraint on the mapped event property is the way to silence it honestly.

The reference page lists every strategy with its factory and encoding: [Generated values and command responses](../reference/generated-values-rendering.md). The same strategies exist in C# as `GenerateValue` in Fundamentals: see [generating values](/fundamentals/csharp/generating-values/).

## When not to generate

Generation is the right default for identities your system owns. It is the wrong tool in four situations.

**The client already has the identifier.** An offline-first app, an importer and a client that retries safely all need to name the thing before the server sees it. That identifier is an input, not a `generated` property. Generation supplies neither idempotency nor deduplication; enforce uniqueness with a Chronicle [constraint](/chronicle/constraints/) or a unique event contract.

**The value is a natural key.** An email address, a SKU or an invoice number already exists in the domain. Keep it as an ordinary property with a constraint and give the entity a surrogate identity if the key can change.

**The value must be guessable or gapless.** A number a customer reads over the phone, or a sequence with no holes, needs an allocator, not randomness. `generate sequence` is reserved and refused until Screenplay admits it. Short random codes also look friendly while carrying few bits, and Screenplay warns below 64.

**The value is a secret and an identity.** Never use an API key, token or password as an event-source id. Event-source ids are plaintext metadata that cannot be encrypted, and every event on the stream repeats it. Use a separate identity such as `ApiKeyId` and keep the secret in a protected property, as the shop does.

## Where to go next

- [Generated values and command responses](../reference/generated-values-rendering.md) is the terse reference for strategies, freshness, fixtures and refusals.
- [Compliance rendering](../reference/compliance-rendering.md) covers `secret`, `pii` and the identity refusals.
- Screenplay owns the language: [generated values](/screenplay/generated-values/) and [choosing a strategy](/screenplay/generated-values/choosing/).
- Fundamentals owns the C# primitives: [generating values](/fundamentals/csharp/generating-values/).
