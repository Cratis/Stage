// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

/// <summary>
/// The events one event source id starts with, in declaration order.
/// </summary>
/// <param name="EventSourceId">The event source id the events are appended to.</param>
/// <param name="Facts">The facts to append.</param>
internal sealed record SemanticSeedGroup(string EventSourceId, ImmutableArray<SemanticFact> Facts);

/// <summary>
/// The events a model's "seed" blocks declare, lowered to facts, and what could not be lowered.
/// </summary>
/// <param name="Groups">The groups, one per event source id in first-declared order; a source declared in several blocks accumulates.</param>
/// <param name="Diagnostics">The seed entries that were left out, with the reason.</param>
internal sealed record SemanticSeedPlan(ImmutableArray<SemanticSeedGroup> Groups, ImmutableArray<SemanticModelLoadDiagnostic> Diagnostics)
{
    internal const string UnresolvedEvent = "STAGE-SEED-001";
    internal const string UnresolvedProperty = "STAGE-SEED-002";
    internal const string UnsupportedValue = "STAGE-SEED-003";
    internal const string NotReplayable = "STAGE-SEED-005";
    internal const string Unreadable = "STAGE-SEED-004";

    internal static readonly SemanticSeedPlan Empty = new([], []);

    internal bool IsEmpty => Groups.IsEmpty;

    /// <summary>
    /// Gets when the seed events occurred; every seed event carries the same occurrence.
    /// </summary>
    internal DateTimeOffset Occurred { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Lowers the "seed" blocks of a Screenplay model (docs/screenplay/seeding.md) to facts the runtime can append.
/// </summary>
/// <remarks>
/// The semantic model deliberately does not carry seeds - they are operational data, not behavior - so the Host reads them from the same source
/// it compiled the model from. A seed entry is only ever left out whole: a half-seeded event would put a wrong fact in the log.
/// </remarks>
internal static class SemanticSeeds
{
    /// <summary>
    /// Reads the seed blocks of a model's source.
    /// </summary>
    /// <param name="modelPath">A .play file or a folder of .play sources.</param>
    /// <param name="plan">The execution plan compiled from the same source.</param>
    /// <returns>The lowered seeds.</returns>
    internal static SemanticSeedPlan Read(string modelPath, SemanticExecutionPlan plan)
    {
        try
        {
            var compiler = new PlayFileCompiler();
            var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileFile(modelPath).Result;
            return result.Success
                ? Admit(Create(result.Value!.Seeds, plan), plan)
                : new([], [Warning(SemanticSeedPlan.Unreadable, "The model's seed blocks could not be read because the source did not compile again.")]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            return new([], [Warning(SemanticSeedPlan.Unreadable, $"The model's seed blocks could not be read: {exception.Message}")]);
        }
    }

    /// <summary>
    /// Lowers seed blocks to facts.
    /// </summary>
    /// <param name="seeds">The seed blocks of the document.</param>
    /// <param name="plan">The execution plan the events resolve against.</param>
    /// <returns>The lowered seeds.</returns>
    internal static SemanticSeedPlan Create(IEnumerable<SeedSyntax>? seeds, SemanticExecutionPlan plan)
    {
        var diagnostics = new List<SemanticModelLoadDiagnostic>();
        var groups = new List<(string Source, List<SemanticFact> Facts)>();
        foreach (var group in (seeds ?? []).SelectMany(seed => seed.Groups))
        {
            var facts = new List<SemanticFact>();
            foreach (var entry in group.Events)
            {
                if (Lower(group.EventSourceId, entry, plan, diagnostics) is { } fact)
                {
                    facts.Add(fact);
                }
            }

            var existing = groups.FindIndex(candidate => candidate.Source == group.EventSourceId);
            if (existing >= 0)
            {
                groups[existing].Facts.AddRange(facts);
            }
            else if (facts.Count > 0)
            {
                groups.Add((group.EventSourceId, facts));
            }
        }

        return new([.. groups.Where(group => group.Facts.Count > 0).Select(group => new SemanticSeedGroup(group.Source, [.. group.Facts]))], [.. diagnostics]);
    }

    /// <summary>
    /// Keeps the seed groups the runtime can replay from the event log.
    /// </summary>
    /// <param name="seeds">The lowered seeds.</param>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The groups whose events the world reconstruction accepts, and the reason for each that it does not.</returns>
    /// <remarks>
    /// The runtime reconstructs its world from the log, and refuses the whole application when a stored event does not match what the model
    /// produces (a missing required value, an event nothing produces). Seeding is only worth it when that cannot happen, so each group is
    /// replayed through the same reconstruction before anything is appended; a group it refuses is left out whole.
    /// </remarks>
    internal static SemanticSeedPlan Admit(SemanticSeedPlan seeds, SemanticExecutionPlan plan)
    {
        var accepted = new List<SemanticSeedGroup>();
        var stored = new List<AppendedEventResponse>();
        var diagnostics = seeds.Diagnostics.ToList();
        foreach (var group in seeds.Groups)
        {
            var trial = stored.Concat(Stored(plan, group.Facts, (ulong)stored.Count, seeds.Occurred)).ToArray();
            try
            {
                SemanticWorldRebuilder.Create(plan, trial, (ulong)(trial.Length - 1), exact: false);
                stored = [.. trial];
                accepted.Add(group);
            }
            catch (SemanticWorldRebuildRefused exception)
            {
                diagnostics.Add(Warning(SemanticSeedPlan.NotReplayable, $"The seed events for '{group.EventSourceId}' cannot be replayed from the event log, so they are not seeded: {exception.Message}"));
            }
        }

        return seeds with { Groups = [.. accepted], Diagnostics = [.. diagnostics] };
    }

    internal static SemanticCommandOccurrence Occurrence(DateTimeOffset occurred) => new(occurred, SemanticChronicleRegistration.SeedSubject, SemanticChronicleRegistration.SeedSubject, SemanticChronicleRegistration.SeedSubject);

    /// <summary>
    /// Gets the stored form of seed facts as they sit in the event log, in sequence from a first sequence number.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="facts">The facts, in append order.</param>
    /// <param name="first">The sequence number of the first fact.</param>
    /// <param name="occurred">The occurrence the facts were appended with.</param>
    /// <returns>The stored events.</returns>
    internal static IEnumerable<AppendedEventResponse> Stored(SemanticExecutionPlan plan, IEnumerable<SemanticFact> facts, ulong first, DateTimeOffset occurred) =>
        facts.Select((fact, index) => Stored(plan, fact, first + (ulong)index, occurred));

    static AppendedEventResponse Stored(SemanticExecutionPlan plan, SemanticFact fact, ulong sequenceNumber, DateTimeOffset occurred) => new()
    {
        Context = new()
        {
            EventType = new() { Id = SemanticStoredEventNames.Of(plan.Events[fact.EventContract]), Generation = 1 },
            EventSourceId = SemanticJsonValues.ToObject(fact.Destination)?.ToString() ?? string.Empty,
            SequenceNumber = sequenceNumber,
            EventSourceType = EventSourceType.Default.Value,
            EventStreamType = EventStreamType.All.Value,
            EventStreamId = EventStreamId.Default,
            Occurred = (Cratis.Chronicle.Contracts.Primitives.SerializableDateTimeOffset?)occurred ?? new(),
            Tags = [.. fact.Tags],
            CausedBy = new() { Subject = SemanticChronicleRegistration.SeedSubject, Name = SemanticChronicleRegistration.SeedSubject, UserName = SemanticChronicleRegistration.SeedSubject }
        },
        Content = SemanticFactAppender.Content(plan, fact)
    };

    static SemanticFact? Lower(string eventSourceId, SeedEventSyntax entry, SemanticExecutionPlan plan, List<SemanticModelLoadDiagnostic> diagnostics)
    {
        var name = entry.Event[(entry.Event.LastIndexOf('.') + 1)..];
        var candidates = plan.Events.Values.Where(@event => @event.Name == name).ToArray();
        if (candidates.Length != 1)
        {
            diagnostics.Add(Warning(
                SemanticSeedPlan.UnresolvedEvent,
                candidates.Length == 0
                    ? $"Seed event '{entry.Event}' for '{eventSourceId}' is not an event of the model, so it is not seeded."
                    : $"Seed event '{entry.Event}' for '{eventSourceId}' names {candidates.Length} events of the model, so it is not seeded.",
                entry.Location));
            return null;
        }

        var contract = candidates[0];
        var values = new List<SemanticPropertyValue>();
        foreach (var mapping in entry.Properties)
        {
            var property = contract.Properties.FirstOrDefault(candidate => candidate.Name == mapping.Property)
                ?? contract.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, mapping.Property, StringComparison.OrdinalIgnoreCase));
            if (property is null)
            {
                diagnostics.Add(Warning(SemanticSeedPlan.UnresolvedProperty, $"Seed event '{entry.Event}' for '{eventSourceId}' sets '{mapping.Property}', which the event does not declare, so it is not seeded.", mapping.Location));
                return null;
            }

            if (!TryJson(mapping.Source, out var json))
            {
                diagnostics.Add(Warning(SemanticSeedPlan.UnsupportedValue, $"Seed event '{entry.Event}' for '{eventSourceId}' sets '{mapping.Property}' to a value that is not a literal, so it is not seeded.", mapping.Location));
                return null;
            }

            try
            {
                values.Add(new(property.Id, SemanticJsonValues.Bind(CanonicalDateTimes(json, property.Type, plan.Model.Application), property.Type, plan.Model)));
            }
            catch (UnsupportedSemanticValue exception)
            {
                diagnostics.Add(Warning(SemanticSeedPlan.UnsupportedValue, $"Seed event '{entry.Event}' for '{eventSourceId}' sets '{mapping.Property}' to a value the event cannot hold ({exception.Message}), so it is not seeded.", mapping.Location));
                return null;
            }
        }

        return new(contract.Id, SemanticValue.Text(eventSourceId), [.. values]) { Tags = contract.Tags };
    }

    // The event log keeps a date and time in the round-trip form ("2026-10-10T08:00:00.0000000Z") and the runtime refuses to rebuild
    // from any other, so a seed written the way a person writes it ("2026-10-10T08:00:00Z") is stored in that form.
    static JsonElement CanonicalDateTimes(JsonElement json, SemanticTypeReference type, SemanticApplication application)
    {
        if (json.ValueKind == JsonValueKind.Array && type.IsCollection)
        {
            var items = json.EnumerateArray().Select(item => CanonicalDateTimes(item, type with { IsCollection = false, IsOptional = false }, application)).ToArray();
            return JsonSerializer.SerializeToElement(items);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && json.ValueKind == JsonValueKind.Object)
        {
            var composite = application.Types.SingleOrDefault(candidate => candidate.Id == type.Target);
            if (composite is null) return json;
            var members = json.EnumerateObject().ToDictionary(
                member => member.Name,
                member => composite.Properties.FirstOrDefault(property => property.Name == member.Name) is { } property ? CanonicalDateTimes(member.Value, property.Type, application) : member.Value);
            return JsonSerializer.SerializeToElement(members);
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.SingleOrDefault(candidate => candidate.Id == type.Target)?.Primitive
            : type.Primitive;
        return primitive == SemanticPrimitiveType.DateTime
            && json.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(json.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? JsonSerializer.SerializeToElement(parsed.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
                : json;
    }

    static bool TryJson(ExpressionSyntax expression, out JsonElement json)
    {
        json = default;
        if (!TryTree(expression, out var tree))
        {
            return false;
        }

        json = JsonSerializer.SerializeToElement(tree);
        return true;
    }

    static bool TryTree(ExpressionSyntax expression, out object? tree)
    {
        tree = null;
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                tree = Literal(literal.Value);
                return true;
            case ListExpressionSyntax list:
                var items = new List<object?>();
                foreach (var item in list.Items)
                {
                    if (!TryTree(item, out var element))
                    {
                        return false;
                    }

                    items.Add(element);
                }

                tree = items;
                return true;
            case ObjectExpressionSyntax @object:
                var members = new Dictionary<string, object?>();
                foreach (var member in @object.Members)
                {
                    if (!TryTree(member.Value, out var value))
                    {
                        return false;
                    }

                    members[member.Name] = value;
                }

                tree = members;
                return true;
            default:
                return false;
        }
    }

    // Numbers arrive as exact numbers in an exact document and as doubles in a legacy one. Either way the value is a decimal without
    // insignificant zeros, so a whole number is appended as a whole number and an integer property's schema accepts it.
    static object? Literal(object? value) => value switch
    {
        ExactNumber exact => Normalized(exact.Value),
        double number when double.IsFinite(number) => Normalized((decimal)number),
        float or int or long or short or byte or decimal => Normalized(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
        _ => value
    };

    static decimal Normalized(decimal value) => value / 1.0000000000000000000000000000m;

    static SemanticModelLoadDiagnostic Warning(string code, string message, SourceLocation? location = null) =>
        new(code, message, location is null ? null : $"{location.Path}({location.Line},{location.Column})") { Severity = DiagnosticSeverity.Warning };
}
