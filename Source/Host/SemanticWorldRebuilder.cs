// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal static class SemanticWorldRebuilder
{
    internal static SemanticWorld Create(
        SemanticExecutionPlan plan,
        IEnumerable<AppendedEventResponse> events,
        ulong tail,
        bool exact = true,
        SemanticBodyExecution? bodies = null)
    {
        var history = events.OrderBy(@event => @event.Context.SequenceNumber).ToArray();
        if (history.Length == 0 || history[0].Context.SequenceNumber != 0 ||
            history[^1].Context.SequenceNumber != tail ||
            history.Where((@event, index) => @event.Context.SequenceNumber != (ulong)index).Any())
        {
            throw new SemanticWorldRebuildRefused($"The event log cannot be read completely through tail {tail}.");
        }

        var facts = history.Select(@event => Fact(plan, @event, exact)).ToImmutableArray();
        SemanticRebuildConstraints.Check(plan, facts);
        try
        {
            return Outcome((bodies is null ? new SemanticEvaluator() : new SemanticEvaluator(bodies, bodies.Bodies)).EstablishWorld(SemanticProjectionAdmission.ForReplay(plan), facts));
        }
        catch (InvalidSemanticContract exception)
        {
            throw new SemanticWorldRebuildRefused($"The stored history is not a valid semantic world: {exception.Message}");
        }
    }

    internal static SemanticWorld Outcome(SemanticExecutionResult result) => result switch
    {
        SemanticAccepted accepted => accepted.World,
        SemanticUnsupported unsupported => throw new SemanticWorldRebuildRefused($"The semantic world cannot project its history: {unsupported.Details}"),
        SemanticRejected rejected => throw new SemanticWorldRebuildRefused($"The stored history is not a valid semantic world: {rejected.Details}"),
        _ => throw new SemanticWorldRebuildRefused("The semantic world reconstruction returned an unknown outcome.")
    };

    /// <summary>
    /// Reads a stored event as a semantic fact.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="stored">The stored event.</param>
    /// <param name="exact">
    /// Whether the event must be the exact storage form of a modeled production - what a restart verifies. Events the host has just appended
    /// itself, from memory, are not read back through storage, so they need not round-trip it (a decimal or a date time) nor name a producer.
    /// Stored seed events use the same non-exact checks as seed admission, identified by their causation subject.
    /// </param>
    /// <returns>The fact.</returns>
    /// <exception cref="SemanticWorldRebuildRefused">The stored event is not one the model can have produced.</exception>
    internal static SemanticFact Fact(SemanticExecutionPlan plan, AppendedEventResponse stored, bool exact = true)
    {
        var context = stored.Context;
        exact = exact && context.CausedBy?.Subject != SemanticChronicleRegistration.SeedSubject;
        if (context.EventType.Generation != 1 || stored.Revisions.Any() ||
            stored.GenerationalContent.Any(entry => entry.Key != 1))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has an unsupported generation or revision.");
        }

        var contracts = plan.Events.Values.Where(candidate => SemanticStoredEventNames.Of(candidate) == context.EventType.Id).ToArray();
        if (contracts.Length != 1)
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has unknown or ambiguous type '{context.EventType.Id}'.");
        }

        var contract = contracts[0];
        var route = Route(plan, context);
        if (exact)
        {
            foreach (var property in contract.Properties)
            {
                EnsureExactStorageType(property.Name, property.Type, plan.Model.Application);
            }
        }

        var producers = Productions(plan, contract.Id).ToArray();
        if (exact && producers.Length == 0 && plan.Projections.Values.Any(projection => projection.Target == SemanticProjectionTargetKind.Event && projection.ReadModel == contract.Id))
        {
            // The reference evaluator cannot yet re-establish a history that holds published public events; refuse explicitly rather than guess.
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} is a local publication of public event '{contract.Name}'; the semantic sandbox cannot yet rebuild a history with published events after a restart. Start a fresh session.");
        }

        var destinationType = route is null ? null : plan.Model.Application.EventSources
            .Single(source => source.SourceKind == route.SourceKind).IdentifierType;
        if (destinationType is null)
        {
            // Screenplay falls back across all producers, not just those on the stored route.
            var destinations = DestinationTypes(plan, contract.Id, []).Distinct().ToArray();
            if (destinations is [{ IsOptional: false, IsCollection: false }])
            {
                destinationType = destinations[0]!;
            }
            else if (exact)
            {
                throw new SemanticWorldRebuildRefused($"Event '{contract.Name}' has no unambiguous typed destination.");
            }
            else
            {
                // An event the host appended itself - seed data - need not be what a command produces; its source id is then plain text.
                destinationType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            }
        }
        var destination = Text(context.EventSourceId, destinationType, plan.Model.Application);
        if ((DateTimeOffset?)context.Occurred is not { } occurred)
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has no occurrence time.");
        }

        using var content = JsonDocument.Parse(stored.Content);
        var values = Values(content.RootElement, contract.Properties, plan.Model.Application);
        if (exact && !producers.Any(producer => RouteMatches(plan, producer.Route, route)))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has a route inconsistent with every modeled production of '{contract.Name}'.");
        }

        if (exact && !producers.Any(producer => RouteMatches(plan, producer.Route, route) && ProductionMatches(producer.Produced, contract, values, context, occurred)))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has tags or occurrence values inconsistent with every modeled production of '{contract.Name}'.");
        }

        return new SemanticFact(contract.Id, destination, values)
        {
            Context = plan.Model.SemanticVersion != SemanticVersion.V1 ? new(new(destinationType, destination)) : null,
            Tags = [.. context.Tags],
            Occurred = occurred,
            Route = route
        };
    }

    internal static ImmutableArray<SemanticPropertyValue> Values(JsonElement json, ImmutableArray<SemanticProperty> properties, SemanticApplication application)
    {
        if (!MatchesSchema(json, properties))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content does not match its semantic property schema.");
        }

        return [.. properties.Select(property => new SemanticPropertyValue(property.Id, PropertyValue(json, property, application)))];
    }

    static SemanticEventRoute? Route(SemanticExecutionPlan plan, Cratis.Chronicle.Contracts.Sequences.EventContext context)
    {
        if (context.EventSourceType == EventSourceType.Default.Value &&
            context.EventStreamType == EventStreamType.All.Value && context.EventStreamId == EventStreamId.Default)
        {
            return null;
        }

        var source = (plan.Model.Application.EventSources.IsDefault ? [] : plan.Model.Application.EventSources)
            .SingleOrDefault(candidate => candidate.SourceKind == context.EventSourceType);
        var stream = source?.Streams.SingleOrDefault(candidate => candidate.StreamKind == context.EventStreamType)
            ?? throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has an unknown stored event-source route.");
        var keyed = stream.StreamIdType is not null || !stream.StreamIdParts.IsDefaultOrEmpty;

        return new(
            context.EventSourceType,
            context.EventStreamType,
            !keyed && context.EventStreamId == EventStreamId.Default ? null : context.EventStreamId);
    }

    // Screenplay 4.141 settlement creates unrouted reaction productions, even from a routed cause.
    static IEnumerable<(SemanticProducedEvent Produced, SemanticCommandRoute? Route)> Productions(SemanticExecutionPlan plan, SemanticId eventContract) =>
        plan.Commands.Values.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => (produced, produced.Route ?? command.Route)))
            .Concat(plan.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(ReactionProductions)
                .Where(produced => produced.EventContract == eventContract)
                .Select(produced => (produced, (SemanticCommandRoute?)null)));

    static IEnumerable<SemanticTypeReference?> DestinationTypes(SemanticExecutionPlan plan, SemanticId eventContract, ImmutableHashSet<SemanticId> visited)
    {
        if (visited.Contains(eventContract))
        {
            return [];
        }

        var path = visited.Add(eventContract);
        var commands = plan.Commands.Values.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => produced.Destination is SemanticResolvedExpression resolved
                ? command.Properties.Single(property => property.Id == resolved.Target).Type
                : command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type));
        var reactions = plan.Reactions.SelectMany(reaction => reaction.Triggers)
            .Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event)
            .SelectMany(trigger => ReactionProductions(trigger).Where(produced => produced.EventContract == eventContract)
                .SelectMany(produced => produced.DestinationType is { } type
                    ? [type]
                    : DestinationTypes(plan, trigger.Source, path)));

        return commands.Concat(reactions);
    }

    static IEnumerable<SemanticProducedEvent> ReactionProductions(SemanticReactionTrigger trigger) =>
        trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces)));

    static bool RouteMatches(SemanticExecutionPlan plan, SemanticCommandRoute? productionRoute, SemanticEventRoute? route)
    {
        if (route is null)
        {
            return productionRoute is null;
        }

        if (productionRoute is not { } declaration)
        {
            return false;
        }

        var source = plan.Model.Application.EventSources.Single(candidate => candidate.Id == declaration.Source);
        var stream = source.Streams.Single(candidate => candidate.Id == declaration.Stream);
        if (source.SourceKind != route.SourceKind || stream.StreamKind != route.StreamKind)
        {
            return false;
        }

        if (declaration.StreamId is SemanticValueExpression literal)
        {
            return TryFormatLiteral(ScalarKind(stream.StreamIdType!, plan.Model.Application), literal.Value, out var formatted) && formatted == route.StreamId;
        }

        if (!declaration.StreamIdParts.IsDefaultOrEmpty && route.StreamId is { } encoded)
        {
            var kinds = stream.StreamIdParts.Select(part => ScalarKind(part.Type, plan.Model.Application)).ToArray();
            if (!SemanticStreamIdFormatter.TryDecodeComposite(encoded, kinds, out var decoded, out _))
            {
                return false;
            }

            return stream.StreamIdParts.Select((part, index) => (declaration.StreamIdParts.Single(mapping => mapping.Part == part.Name).Value, Index: index))
                .Where(part => part.Value is SemanticValueExpression)
                .All(part => TryFormatLiteral(kinds[part.Index], ((SemanticValueExpression)part.Value).Value, out var formatted) && formatted == decoded![part.Index]);
        }

        return true;
    }

    static StreamIdScalarKind ScalarKind(SemanticTypeReference type, SemanticApplication application) =>
        (type.Kind == SemanticTypeReferenceKind.Concept ? application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive) switch
        {
            SemanticPrimitiveType.Uuid => StreamIdScalarKind.Uuid,
            SemanticPrimitiveType.WholeNumber => StreamIdScalarKind.WholeNumber,
            SemanticPrimitiveType.Text => StreamIdScalarKind.Text,
            _ => throw new SemanticWorldRebuildRefused("A stored route has an unsupported stream identity type.")
        };

    static bool TryFormatLiteral(StreamIdScalarKind kind, SemanticValue value, out string? formatted)
    {
        formatted = null;
        return (kind, value) switch
        {
            (StreamIdScalarKind.Text, SemanticTextValue text) => SemanticStreamIdFormatter.TryFormatText(text.Value, out formatted, out _),
            (StreamIdScalarKind.Uuid, SemanticTextValue text) => SemanticStreamIdFormatter.TryFormatUuidText(text.Value, out formatted, out _),
            (StreamIdScalarKind.WholeNumber, SemanticNumberValue number) when decimal.Truncate(number.Value) == number.Value =>
                SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out formatted, out _),
            _ => false
        };
    }

    static void EnsureExactStorageType(string path, SemanticTypeReference type, SemanticApplication application)
    {
        if (type.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            foreach (var property in application.Types.Single(candidate => candidate.Id == type.Target).Properties)
            {
                EnsureExactStorageType($"{path}.{property.Name}", property.Type, application);
            }

            return;
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(candidate => candidate.Id == type.Target).Primitive
            : type.Primitive;
        if (primitive is SemanticPrimitiveType.DateTime or SemanticPrimitiveType.DecimalNumber)
        {
            throw new SemanticWorldRebuildRefused($"Event property '{path}' uses {primitive}, which Chronicle storage cannot round-trip exactly.");
        }
    }

    static bool ProductionMatches(
        SemanticProducedEvent produced,
        SemanticEventContract contract,
        ImmutableArray<SemanticPropertyValue> values,
        Cratis.Chronicle.Contracts.Sequences.EventContext context,
        DateTimeOffset occurred)
    {
        if (!contract.Tags.AddRange(produced.Tags).SequenceEqual(context.Tags))
        {
            return false;
        }

        foreach (var mapping in produced.Mappings.Where(mapping => mapping.Source is SemanticEventContextExpression))
        {
            var actual = values.Single(value => value.TargetProperty == mapping.TargetProperty).Value;
            var expected = ((SemanticEventContextExpression)mapping.Source).Value switch
            {
                SemanticEventContextValueKind.Occurred => occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                SemanticEventContextValueKind.CausedBySubject => context.CausedBy?.Subject,
                SemanticEventContextValueKind.CausedByName => context.CausedBy?.Name,
                SemanticEventContextValueKind.CausedByUserName => context.CausedBy?.UserName,
                _ => null
            };
            if (expected is null || actual is not SemanticTextValue text || text.Value != expected)
            {
                return false;
            }
        }

        return true;
    }

    static ImmutableArray<SemanticPropertyValue> CompositeValues(JsonElement json, ImmutableArray<SemanticProperty> properties, SemanticApplication application)
    {
        if (!MatchesSchema(json, properties))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content does not match its semantic property schema.");
        }

        // Composite absence and supplied order are semantic data, not missing top-level event columns.
        return [.. json.EnumerateObject().Select(member =>
        {
            var property = properties.Single(property => property.Name == member.Name);
            return new SemanticPropertyValue(property.Id, Value(member.Value, property.Type, application));
        })];
    }

    // Every stored name is declared exactly once and appears once. Chronicle may omit an absent optional value
    // (it does for protected concepts), so only optional properties may be missing.
    static bool MatchesSchema(JsonElement json, ImmutableArray<SemanticProperty> properties)
    {
        if (json.ValueKind != JsonValueKind.Object) return false;
        var names = json.EnumerateObject().Select(entry => entry.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) return false;
        if (names.Any(name => properties.Count(property => property.Name == name) != 1)) return false;

        return properties.All(property => property.Type.IsOptional || names.Contains(property.Name, StringComparer.Ordinal));
    }

    static SemanticValue PropertyValue(JsonElement json, SemanticProperty property, SemanticApplication application) =>
        json.TryGetProperty(property.Name, out var value) ? Value(value, property.Type, application) : SemanticValue.Null;

    static int EnumerationIndex(SemanticConcept concept, int number)
    {
        if (concept.Numbers is { } numbers) return numbers.IndexOf(number);

        return number >= 0 && number < concept.Values.Length ? number : -1;
    }

    static SemanticValue Text(string value, SemanticTypeReference type, SemanticApplication application) => Value(JsonSerializer.SerializeToElement(value), type, application);

    static SemanticValue Value(JsonElement json, SemanticTypeReference type, SemanticApplication application)
    {
        if (json.ValueKind == JsonValueKind.Null && type.IsOptional)
        {
            return SemanticValue.Null;
        }

        if (type.IsCollection && json.ValueKind == JsonValueKind.Array)
        {
            return SemanticValue.Array([.. json.EnumerateArray().Select(item => Value(item, type with { IsCollection = false, IsOptional = false }, application))]);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && json.ValueKind == JsonValueKind.Object)
        {
            var composite = application.Types.Single(candidate => candidate.Id == type.Target);
            return SemanticValue.Composite(CompositeValues(json, composite.Properties, application));
        }

        var concept = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(candidate => candidate.Id == type.Target)
            : null;
        if (concept?.Values is { IsEmpty: false } allowed)
        {
            return json.ValueKind switch
            {
                JsonValueKind.Number when json.TryGetInt32(out var number) && EnumerationIndex(concept, number) is var index && index >= 0 => SemanticValue.Text(allowed[index]),
                JsonValueKind.String when allowed.Contains(json.GetString()!, StringComparer.Ordinal) => SemanticValue.Text(json.GetString()!),
                JsonValueKind.String => throw new SemanticWorldRebuildRefused("Chronicle content contains a value outside its concept enumeration."),
                _ => throw new SemanticWorldRebuildRefused("Chronicle content contains a value incompatible with its semantic type.")
            };
        }

        var primitive = concept?.Primitive ?? type.Primitive;
        return (primitive, json.ValueKind) switch
        {
            (SemanticPrimitiveType.Text, JsonValueKind.String) => SemanticValue.Text(json.GetString()!),
            (SemanticPrimitiveType.Uuid, JsonValueKind.String) when Guid.TryParseExact(json.GetString(), "D", out var guid) && guid.ToString("D") == json.GetString() => SemanticValue.Text(json.GetString()!),
            (SemanticPrimitiveType.Date, JsonValueKind.String) when DateOnly.TryParseExact(json.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) => SemanticValue.Text(json.GetString()!),
            (SemanticPrimitiveType.DateTime, JsonValueKind.String) when DateTimeOffset.TryParseExact(json.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) => SemanticValue.Text(json.GetString()!),
            (SemanticPrimitiveType.Boolean, JsonValueKind.True) => SemanticValue.Boolean(true),
            (SemanticPrimitiveType.Boolean, JsonValueKind.False) => SemanticValue.Boolean(false),
            (SemanticPrimitiveType.DecimalNumber, JsonValueKind.Number) when json.TryGetDecimal(out var decimalNumber) => SemanticValue.Number(decimalNumber),
            (SemanticPrimitiveType.WholeNumber, JsonValueKind.Number) when json.TryGetDecimal(out var wholeNumber) && decimal.Truncate(wholeNumber) == wholeNumber => SemanticValue.Number(wholeNumber),
            _ => throw new SemanticWorldRebuildRefused("Chronicle content contains a value incompatible with its semantic type.")
        };
    }
}
