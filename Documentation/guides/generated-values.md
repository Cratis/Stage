---
title: 'Cratis Stage: generate identifiers and values'
description: Declare generated values in Screenplay, render them with Cratis Stage, and check how Arc creates identifiers and Chronicle guards new event sources.
---

Use Cratis Stage generated values when a command should create an identifier or code rather than accept it from the caller. Declare the generation strategy in Screenplay, then let Stage render the C# that Arc uses to create the value before the command handler runs.

A concept is a named domain type, such as `CustomerId`. It owns the strategy: `generate ulid`, `generate typeid "order"` or `generate nanoid`. The command property says `generated` to use that strategy. The shop examples below distinguish identifiers for event sources from values stored inside events. An event source is the identity whose events Chronicle groups together.

| Value | Concept | What it is | Strategy |
| --- | --- | --- | --- |
| `CustomerId` | `cus_` + ULID | Event source of a customer | `ulid` with a prefix |
| `OrderId` | `order_` + TypeID suffix | Event source of an order | `typeid "order"` |
| `InviteCode` | A short opaque code | A value inside an event | `nanoid` |
| `ApiKeyId` | A UUID | Event source of an API key | Implicit `uuid` |
| `ApiKey` | A secret | A protected value inside an event | None. A caller supplies it |

Start with a model you can already compile and a .NET SDK compatible with the rendered project. The listings are excerpts, not a complete shop application; use your own complete model when following the rendering steps. [Plan artifacts from Screenplay sources](../reference/plan-from-sources.md) covers the planning API and setup. You will check the generated request shape, pin values in specifications, and choose whether generation fits each value.

## Declare the strategy on the concept

Put the strategy on `CustomerId`, then mark the command property as a generated identifier. This keeps the identifier's format consistent wherever a command generates that concept.

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

The caller sends a name, not a `customerId`. Arc creates the identifier using `CustomerId`'s strategy, and `returns customerId` sends it back as the command response. There is no identifier-generation code to add to the handler.

`registeredAt DateTime = $context.occurred` records the command's receipt time in the event. Time-dependent strategies use that same captured time, as shown below.

Screenplay checks the strategy while compiling. A generated identifier needs at least 64 random bits, an unsupported strategy is refused, and the model's semantic version rises to ESM v15 as soon as one concept serializes a strategy. The [Screenplay generated values page](/screenplay/generated-values/) owns the syntax and the compiler's rules.

## Render it with Stage

Stage returns a plan of artifact paths and bytes; your caller writes them. The console example below references the `Rendering.Cratis` project under this repository's `Source/` folder and calls `CratisRendering.PlanFrom`, described in [Plan artifacts from Screenplay sources](../reference/plan-from-sources.md).

Use a new, empty output folder for this example. Its write loop overwrites every planned path, including scaffold paths. `CheckPublication` checks event identities, policy layout and required removals; it does not protect hand-edited files or provide a complete publication transaction.

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

After building the helper, pass the folder holding your complete model and the empty destination. This command assumes the helper assembly is named `DocsGvRender.dll` and was built at the path shown:

```bash
dotnet render/bin/Debug/net10.0/DocsGvRender.dll shop out
```

The console prints planning and publication results. This is an illustrative result for a complete shop model; artifact counts depend on the model and Stage version.

```text title="Output"
Success=True artifacts=17 scaffold=40
Publication check: Compatible { }
```

`Success=True` means planning produced no error diagnostics. A compatible publication result means the plan passed Stage's destination checks, not that every overwrite is safe. For example, an event type id change can be refused with `STAGE-EVENT-002`. [Build a renderer target](build-renderer-target.md) explains the ownership and write safeguards a publisher must add.

Inspect your plan for the concept, command and specification files. A complete version of the shop can produce paths like these alongside the scaffold:

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
A successful plan means Stage admitted the model; it does not mean the generated application compiles or its specifications pass. Build your rendered application with `dotnet build`, then run `dotnet test`. Check the generated request, response and pinned-value assertions before using the application.
:::

## Read the rendered concepts

Inspect `Common/CustomerId.cs` first. Because `CustomerId` is an `identifier`, its record derives from Chronicle's `EventSourceId<string>`. Its ULID strategy needs the captured command time, so the record implements `IGeneratableInContext<T>` and exposes `New(context)`:

```csharp title="Common/CustomerId.cs"
namespace Acme.Shop.Common;

/// <summary>
/// Represents customer id.
/// </summary>
public record CustomerId(string Value) : EventSourceId<string>(Value), IGeneratableInContext<CustomerId>
{
    public static readonly CustomerId NotSet = new(string.Empty);

    public static CustomerId New(GenerationContext context) => new("cus_" + GenerateValue.Ulid(context).ToLowerInvariant());

    public static implicit operator CustomerId(string value) => new(value);
}
```

`OrderId` has the same shape. Only the factory differs:

```csharp title="Common/OrderId.cs"
namespace Acme.Shop.Common;

/// <summary>
/// Represents order id.
/// </summary>
public record OrderId(string Value) : EventSourceId<string>(Value), IGeneratableInContext<OrderId>
{
    public static readonly OrderId NotSet = new(string.Empty);

    public static OrderId New(GenerationContext context) => new(GenerateValue.TypeId("order", context));

    public static implicit operator OrderId(string value) => new(value);
}
```

`InviteCode` is neither time-ordered nor an identifier. It is a plain value concept with `IGeneratable<T>` and a parameterless `New()`:

```csharp title="Common/InviteCode.cs"
namespace Acme.Shop.Common;

/// <summary>
/// Represents invite code.
/// </summary>
public record InviteCode(string Value) : ConceptAs<string>(Value), IGeneratable<InviteCode>
{
    public static readonly InviteCode NotSet = new(string.Empty);

    public static InviteCode New() => new(GenerateValue.NanoId(21, null));

    public static implicit operator string(InviteCode value) => value.Value;
    public static implicit operator InviteCode(string value) => new(value);
}
```

Time-ordered concepts have no parameterless `New()`. The compiler stops hand-written code from asking for a ULID without saying when. Context-free strategies keep `New()`.

## Read the rendered command and event

The command follows the model. Nothing about the identifier appears in the constructor:

```csharp title="Storefront/Customers/Register/Register.cs"
using Acme.Shop.Common;
using Acme.Shop.GeneratedCommands;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Acme.Shop.Storefront.Customers.Register;

[Command]
[AllowAnonymous]
public record RegisterCustomer(CustomerName Name)
{
    public (CustomerId, EventsWithConcurrencyScopes) Handle([GeneratedValue("customerId", Freshness = GeneratedValueFreshness.Unguarded)] CustomerId customerId, IOperationContextAccessor operation)
    {
        var occurred = CommandReceiptTime.OccurredAtReceipt(operation);
        return (customerId, new EventsWithConcurrencyScopes([new EventForEventSourceId(customerId, new CustomerRegistered(Name, occurred)) { Occurred = occurred }], Enumerable.Select(Enumerable.Distinct<EventSourceId>([customerId]), source => new KeyValuePair<EventSourceId, ConcurrencyScope>(source, new(EventSequenceNumber.BeforeFirst, EventSourceId: source)))));
    }
}

/// <summary>
/// The event that occurs when customer registered.
/// </summary>
[EventType]
public record CustomerRegistered(CustomerName Name, DateTimeOffset RegisteredAt);

```

`RegisterCustomer(CustomerName Name)` is the whole request. Arc supplies the `Handle` parameter marked `[GeneratedValue("customerId")]`; `customerId` is the case-sensitive key from the model. The tuple separates what the caller receives from what Chronicle appends: its first element is the response, and its second contains the events and concurrency scopes.

Each generated destination gets a `ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: source)`, requiring an empty event source at append time. `Freshness = GeneratedValueFreshness.Unguarded` disables Arc's separate freshness check because this handler supplies that append-time guard. `Occurred = occurred` uses the receipt time captured by Arc for the `$context.occurred` mapping.

The generated TypeScript proxy carries the same contract to the browser. It sends `name` only and types the response as the identifier's string:

```typescript title="Shop/Storefront/Customers/Register/Register.ts (excerpt)"
export interface IRegisterCustomer {
    name?: string;
}

export class RegisterCustomer extends Command<IRegisterCustomer, string> implements IRegisterCustomer {
```

The proxy comes from the build, not from the plan: Arc's proxy generator writes it when you compile the rendered project.

## Follow one command through the runtime

Follow `RegisterCustomer` from dispatch to append to see where each guarantee comes from. Generation follows authorization and input validation, so those checks cannot use a value that has not been created yet. A dispatch that reaches unpinned generation draws a new value.

1. Arc captures the receipt time at dispatch using the configured `TimeProvider` and keeps it for that dispatch.
2. Arc resolves the generated parameter. For a time-dependent concept it builds a `GenerationContext` from the receipt time and current tenant, then calls `New(context)`. Context-free concepts use `New()`. It validates the generated result or pin through the concept's validator before calling `Handle`. Unpinned context-dependent generation without a receipt time fails with `MissingGenerationTime`; it does not fall back to the wall clock.
3. `Handle` returns the response with the events and scopes.
4. Chronicle checks the scopes against the state before the batch and appends atomically. A `BeforeFirst` scope requires an empty event source. Several first facts may be appended to that source together; one violated scope rejects the whole batch.
5. If a storage append loses a sequence-number race, Chronicle renumbers the batch and checks the scopes again before retrying. This requires Chronicle 19.40.1 or later; the rendered client pin comes from Stage's `Directory.Packages.props`.
6. Arc puts the tuple's first element in `CommandResult<T>.Response`. A successful command returns the generated identifier, typed as a string in the TypeScript proxy. A `returns` block instead gives a named response record; see [Responses](../reference/generated-values-rendering.md#responses). A reused source produces a validation failure with reason `ConcurrencyViolation`.

The generated rejection specification asserts exactly that reason and that no events were appended:

```csharp title="Storefront/Customers/Register/when_refusing_used_identity.cs (excerpt)"
async Task Establish()
{
    _scenario.PinGeneratedValue("customerId", _customerId);
    await _scenario.EventScenario.Given.ForEventSource(_customerId).Events(new CustomerRegistered(new CustomerName("Ada"), _registeredAt));
    _givenEventCount = _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess);
}

async Task Because() => _result = await _scenario.Execute(new RegisterCustomer(new CustomerName("Grace")));

[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
[Fact] void should_report_the_freshness_violation() => _result.ValidationResults.First().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
[Fact] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);
```

The Chronicle [concurrency page](/chronicle/events/concurrency/) explains scopes and `ConcurrencyViolation` in general.

:::caution[Freshness is not idempotency]
A new unpinned execution generates another identifier, so resending after a timeout can register a second customer if the first request succeeded. Generation does not deduplicate requests. For retries, let the client supply a stable identifier and define how the command handles reuse, with a constraint where needed. See [When not to generate](#when-not-to-generate).
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
public (InviteCode, EventsWithConcurrencyScopes) Handle([GeneratedValue("inviteCode", Freshness = GeneratedValueFreshness.Unguarded)] InviteCode inviteCode)
{
    return (inviteCode, new EventsWithConcurrencyScopes([new EventForEventSourceId(CustomerId, new InviteIssued(inviteCode))], Enumerable.Select(Enumerable.Distinct<EventSourceId>([.. (string.Equals(CustomerId.Value.ToString(), inviteCode.Value, StringComparison.Ordinal) ? new EventSourceId[] { CustomerId } : [])]), source => new KeyValuePair<EventSourceId, ConcurrencyScope>(source, new(EventSequenceNumber.BeforeFirst, EventSourceId: source)))));
}
```

The scope list contains the customer id only when its text equals the generated code. Here the prefixed ULID and default NanoID differ in length, so this command adds no freshness scope for the customer's existing stream. Stage compares values, not concept types; do not rely on different concept names to prevent a match if you change the strategies.

Generating `InviteCode` does not establish its uniqueness. The default 21-character NanoID has 126 random bits, but randomness is not a uniqueness guarantee. Add a constraint on the event property if duplicate codes must be rejected.

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
    internal static DateTimeOffset OccurredAtReceipt(IOperationContextAccessor operation)
    {
        var receivedAt = operation.ReceivedAt ?? throw new CommandReceiptTimeUnavailable();
        if (receivedAt == default)
        {
            throw new CommandReceiptTimeUnavailable();
        }
        return DateTimeOffset.FromUnixTimeMilliseconds(receivedAt.ToUnixTimeMilliseconds());
    }
}
```

The helper truncates the receipt to whole milliseconds, matching the time precision of ULID, UUIDv7 and TypeID. A `registeredAt` mapped from `$context.occurred` therefore agrees with the time prefix of `CustomerId` to the millisecond.

Without an occurrence-time mapping, this generated handler supplies no `Occurred` value and Chronicle assigns append time. Do not assume the identifier's timestamp and an unmapped event timestamp are identical. Time-ordered identifiers also reveal their creation time and are not monotonic within a millisecond; use event ordering when you need a strict sequence.

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

Pin a value when a specification needs to assert the exact identifier, or let Stage assert the generated shape when the particular value does not matter. `RegisteringWithPinnedIdentity` uses `for` to pin the identifier and `given clock` to fix the receipt time:

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
public class when_registering_with_pinned_identity : Specification, IDisposable
{
    readonly CommandScenario<RegisterCustomer> _scenario = new();
    static readonly CustomerId _customerId = new CustomerId("cus_01arz3ndektsv4rrffq69g5fav");
    static readonly CustomerName _name = new CustomerName("Ada");
    static readonly DateTimeOffset _registeredAt = DateTimeOffset.Parse("2026-01-02T03:04:05.6780000Z", CultureInfo.InvariantCulture);
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Clock(new DateTimeOffset(639029198456780000L, TimeSpan.Zero));
        _scenario.PinGeneratedValue("customerId", _customerId);
    }

    async Task Because() => _result = await _scenario.Execute(new RegisterCustomer(_name));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_customer_registered() => await _scenario.ShouldHaveAppendedEvent<RegisterCustomer, CustomerRegistered>(_customerId, @event => true);
    [Fact] async Task should_have_appended_customer_registered_with_name() => await _scenario.ShouldHaveAppendedEvent<RegisterCustomer, CustomerRegistered>(_customerId, @event => @event.Name == _name);
    [Fact] async Task should_have_appended_customer_registered_with_registered_at() => await _scenario.ShouldHaveAppendedEvent<RegisterCustomer, CustomerRegistered>(_customerId, @event => @event.RegisteredAt == _registeredAt);
    [Fact] void should_return_the_expected_response() => ((CommandResult<CustomerId>)_result).Response.ShouldEqual(_customerId);

    public void Dispose() => _scenario.Dispose();
}
```

The pin must match the strategy's exact shape: `cus_` plus a 26-character lowercase ULID whose first character is `0`–`7`. `RegisteringAtTheReceiptTime` pins nothing. Stage sets the scenario clock and allows generation, then asserts the shape of the generated value and its time prefix:

```csharp title="Storefront/Customers/Register/when_registering_at_the_receipt_time.cs (excerpt)"
void Establish()
{
    _scenario.Clock(new DateTimeOffset(639029198456780000L, TimeSpan.Zero));
    _scenario.AllowGeneration();
}
```

The last fact checks the receipt-time prefix:

```csharp title="Storefront/Customers/Register/when_registering_at_the_receipt_time.cs (excerpt)"
[Fact] void should_generate_customer_id_at_the_receipt_time() => _scenario.GeneratedValues.Last<CustomerId>("customerId").Value.ToString()!.StartsWith("cus_01kdyak3se", StringComparison.Ordinal).ShouldBeTrue();
```

`cus_01kdyak3se` is the ULID time prefix for `2026-01-02T03:04:05.678Z`. The random remainder stays unpinned.

## Add rules to a generated concept

You can put ordinary validation rules on a generated concept from ESM v20. For example, the canonical generated-concept rules corpus adds this rule to the same customer-id strategy:

```screenplay
concept CustomerId : String
  generate ulid prefix "cus_" case lower
  validate
    matches "^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$"
```

Stage emits both the factory and a `ConceptValidator<CustomerId>`. Arc checks a factory result or pinned value against that validator before `Handle` runs. A failing rule means generation is defective: it produces a server exception with no input validation results and no appended events. The semantic Host does the same, but only when execution reaches generation after authorization and ordinary input validation. Screenplay's reference and the independent specification executor express the defect as `Rejected(Contract)`.

Pin every rule-bearing generated value in a rendered specification, even for this provable rule. Stage cannot see the compiler's proof status in ESM, so it refuses an unpinned specification with `STAGE-ESM-038` instead of allowing a random failure. A pin failing the strategy shape or rules fails `STAGE-ESM-011`; it is not rendered as a normal validation-error assertion. The command and concept remain renderable without that specification.

Generated concept rules can use error, warning or information severity. Arc rejects a generated value failing any of them as a server generation defect, not an input validation failure. A compiler warning that a rule is *unproven* (`PLAY0689`) is different: pin a valid value to make its rendered specification deterministic. A refuted rule (`PLAY0688`) must be corrected before rendering.

Freshness follows feature use, not the schema version. Adding rules to an implicit UUID concept selects v20 but does not enable freshness by itself. A model with no explicit concept strategy remains unguarded, even when another feature selects v16 or later.

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

```csharp title="Common/ApiKey.cs"
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ProtectedValues;

namespace Acme.Shop.Common;

/// <summary>
/// Represents api key.
/// </summary>
[Encrypted]
[NotAudited]
public record ApiKey(string Value) : ConceptAs<string>(Value)
{
    public static readonly ApiKey NotSet = new(string.Empty);

    public static implicit operator string(ApiKey value) => value.Value;
    public static implicit operator ApiKey(string value) => new(value);
}
```

```csharp title="Storefront/Customers/Keys/Keys.cs (excerpt)"
[Command]
[AllowAnonymous]
public record RegisterApiKey(CustomerId CustomerId, [property: NotAudited] ApiKey ApiKey)
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
Error PLAY0268: Generated concepts with compliance markers are not admitted until #616 phase 4. [refused.play(3,3)]
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

Keep the identifier as an input when the client already owns it. Offline-first applications and importers often need to name an entity before the server sees it. Retrying clients need a stable request identity and defined duplicate handling; generation supplies neither idempotency nor deduplication. Use a Chronicle [constraint](/chronicle/constraints/) or unique event contract where uniqueness is required.

A natural key, such as an email address or an existing SKU, is also an ordinary input. Give the entity a separate surrogate identity if that key can change. For a gapless sequence, use an allocator designed for that requirement. Random generation cannot supply it, and `generate sequence` is reserved and refused by Screenplay.

Never use an API key, token or password as an event-source id. That id is plaintext metadata repeated on every event and cannot be encrypted. Use a separate identity such as `ApiKeyId` and keep the secret in a protected property, as the shop does.

## Where to go next

- [Generated values and command responses](../reference/generated-values-rendering.md) is the terse reference for strategies, freshness, fixtures and refusals.
- [Compliance rendering](../reference/compliance-rendering.md) covers `secret`, `pii` and the identity refusals.
- Screenplay owns the language: [generated values](/screenplay/generated-values/) and [choosing a strategy](/screenplay/generated-values/choosing/).
- Fundamentals owns the C# primitives: [generating values](/fundamentals/csharp/generating-values/).
