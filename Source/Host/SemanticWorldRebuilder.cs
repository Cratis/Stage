// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host;

internal static class SemanticWorldRebuilder
{
    internal static SemanticWorld Create(
        SemanticExecutionPlan plan,
        IEnumerable<AppendedEventResponse> events,
        IReadOnlyDictionary<SemanticId, IReadOnlyList<string>> instances,
        ulong tail)
    {
        var history = events.OrderBy(@event => @event.Context.SequenceNumber).ToArray();
        if (history.Length == 0 || history[0].Context.SequenceNumber != 0 ||
            history[^1].Context.SequenceNumber != tail ||
            history.Where((@event, index) => @event.Context.SequenceNumber != (ulong)index).Any())
        {
            throw new SemanticWorldRebuildRefused($"The event log cannot be read completely through tail {tail}.");
        }

        var facts = history.Select(@event => Fact(plan, @event)).ToImmutableArray();
        SemanticRebuildConstraints.Check(plan, facts);
        var readModels = new List<SemanticReadModelInstance>();
        foreach (var (id, rows) in instances)
        {
            var model = plan.ReadModels[id];
            var identifier = model.Properties.Single(property => property.IsIdentifier);
            foreach (var row in rows)
            {
                using var document = JsonDocument.Parse(row);
                var values = MirrorValues(document.RootElement, model, plan.Model.Application);
                readModels.Add(new(id, values.Single(value => value.TargetProperty == identifier.Id).Value, values));
            }
        }

        try
        {
            var world = SemanticWorld.Create(facts, [.. readModels]);
            SemanticMirrorVerification.Check(plan, facts, world);
            return world;
        }
        catch (InvalidSemanticContract exception)
        {
            throw new SemanticWorldRebuildRefused($"Chronicle's mirror is not a valid semantic world: {exception.Message}");
        }
    }

    internal static SemanticFact Fact(SemanticExecutionPlan plan, AppendedEventResponse stored)
    {
        var context = stored.Context;
        if (context.EventType.Generation != 1 || stored.Revisions.Any() ||
            stored.GenerationalContent.Any(entry => entry.Key != 1))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has an unsupported generation or revision.");
        }

        var contracts = plan.Events.Values.Where(candidate => candidate.Name == context.EventType.Id).ToArray();
        if (contracts.Length != 1)
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has unknown or ambiguous type '{context.EventType.Id}'.");
        }

        var contract = contracts[0];
        foreach (var property in contract.Properties)
        {
            EnsureExactStorageType(property.Name, property.Type, plan.Model.Application);
        }

        var producers = plan.Commands.Values.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == contract.Id)
            .Select(produced => (Command: command, Produced: produced))).ToArray();
        var destinations = producers.Select(producer => producer.Produced.Destination is SemanticResolvedExpression resolved
            ? producer.Command.Properties.Single(property => property.Id == resolved.Target).Type
            : producer.Command.Destination?.Type).Distinct().ToArray();
        if (destinations is not [{ IsOptional: false, IsCollection: false }])
        {
            throw new SemanticWorldRebuildRefused($"Event '{contract.Name}' has no unambiguous typed destination.");
        }

        var destinationType = destinations[0]!;
        var destination = Text(context.EventSourceId, destinationType, plan.Model.Application);
        if ((DateTimeOffset?)context.Occurred is not { } occurred)
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has no occurrence time.");
        }

        using var content = JsonDocument.Parse(stored.Content);
        var values = Values(content.RootElement, contract.Properties, plan.Model.Application);
        if (!producers.Any(producer => ProductionMatches(producer.Produced, contract, values, context, occurred)))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has tags or occurrence values inconsistent with every modeled production of '{contract.Name}'.");
        }

        return new SemanticFact(contract.Id, destination, values)
        {
            Context = plan.Model.SemanticVersion == SemanticVersion.V2 ? new(new(destinationType, destination)) : null,
            Tags = [.. context.Tags]
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

    static ImmutableArray<SemanticPropertyValue> MirrorValues(JsonElement json, SemanticReadModel model, SemanticApplication application)
    {
        if (json.ValueKind != JsonValueKind.Object ||
            json.EnumerateObject().Any(entry => model.Properties.All(property => property.Name != entry.Name) &&
                entry.Name is not ("id" or "__initialized" or "__subject")))
        {
            throw new SemanticWorldRebuildRefused("Chronicle mirror contains an unknown property.");
        }

        var identifier = model.Properties.Single(property => property.IsIdentifier);
        var key = json.GetProperty(identifier.Name).ToString();
        if (!json.TryGetProperty("id", out var id) || id.ToString() != key ||
            !json.TryGetProperty("__initialized", out var initialized) || initialized.ValueKind != JsonValueKind.True)
        {
            throw new SemanticWorldRebuildRefused("Chronicle mirror metadata does not agree with its semantic key.");
        }

        var semantic = JsonSerializer.SerializeToElement(model.Properties.ToDictionary(property => property.Name, property => json.GetProperty(property.Name)));
        return Values(semantic, model.Properties, application);
    }

    static ImmutableArray<SemanticPropertyValue> Values(JsonElement json, ImmutableArray<SemanticProperty> properties, SemanticApplication application)
    {
        if (json.ValueKind != JsonValueKind.Object || json.EnumerateObject().Count() != properties.Length ||
            json.EnumerateObject().Any(entry => properties.Count(property => property.Name == entry.Name) != 1))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content does not match its semantic property schema.");
        }

        return [.. properties.Select(property => new SemanticPropertyValue(
            property.Id, Value(json.GetProperty(property.Name), property.Type, application)))];
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
            return SemanticValue.Composite(Values(json, composite.Properties, application));
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(candidate => candidate.Id == type.Target).Primitive
            : type.Primitive;
        var result = (primitive, json.ValueKind) switch
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
        if (type.Kind == SemanticTypeReferenceKind.Concept && application.Concepts.Single(candidate => candidate.Id == type.Target).Values is { IsEmpty: false } allowed &&
            result is SemanticTextValue text && !allowed.Contains(text.Value, StringComparer.Ordinal))
        {
            throw new SemanticWorldRebuildRefused("Chronicle content contains a value outside its concept enumeration.");
        }

        return result;
    }
}
