// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Semantics;
using ChronicleSequences = Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Stage.Host;

/// <summary>
/// The events one <c language="screenplay">seed</c> group appends to a single event source.
/// </summary>
/// <param name="EventSourceId">The event source id the events are appended to.</param>
/// <param name="Events">The events, in declaration order.</param>
internal sealed record SeedGroup(string EventSourceId, IReadOnlyList<SeedEvent> Events);

/// <summary>
/// One seeded event: the event type's name and its payload.
/// </summary>
/// <param name="Event">The name of the declared event.</param>
/// <param name="Content">The event payload.</param>
internal sealed record SeedEvent(string Event, JsonObject Content);

/// <summary>
/// Appends a model's <c language="screenplay">seed</c> blocks (docs/screenplay/seeding.md) to a fresh event log so a played application starts with the state it declares.
/// </summary>
/// <remarks>
/// The executable semantic model treats seeds as operational metadata and does not carry them, so they are read from the compiled syntax. Seeding runs
/// once, before the semantic world is rebuilt from the log: the seeded events flow through projections and reactions like any other event.
/// </remarks>
internal static class SemanticSeeding
{
    const string Seeder = "Stage seed";

    internal static IReadOnlyList<SeedGroup> Load(string modelPath)
    {
        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileFile(modelPath).Result;
        if (!result.Success || result.Value is null)
        {
            return [];
        }

        return From(result.Value);
    }

    internal static IReadOnlyList<SeedGroup> From(ApplicationSyntax application) =>
    [
        .. (application.Seeds ?? [])
            .SelectMany(seed => seed.Groups)
            .Select(group => new SeedGroup(
                group.EventSourceId,
                [.. group.Events.Select(@event => new SeedEvent(@event.Event, Content(@event.Properties)))]))
            .Where(group => group.Events.Count > 0)
    ];

    /// <summary>
    /// Establishes the semantic world the seeds describe, or says why it cannot.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="seeds">The seeded events.</param>
    /// <param name="refusal">Why the world cannot be established, when it cannot.</param>
    /// <returns>The world, or <see langword="null"/> when the seeds do not describe a valid history.</returns>
    internal static SemanticWorld? Establish(SemanticExecutionPlan plan, IReadOnlyList<SeedGroup> seeds, out string? refusal)
    {
        refusal = null;
        try
        {
            var facts = Facts(plan, seeds);
            if (facts.Length == 0)
            {
                return null;
            }

            return SemanticWorldRebuilder.Outcome(new SemanticEvaluator().EstablishWorld(plan, facts));
        }
        catch (Exception exception) when (exception is SemanticWorldRebuildRefused or InvalidSemanticContract or InvalidOperationException or JsonException or ArgumentException)
        {
            refusal = exception.Message;
            return null;
        }
    }

    internal static ImmutableArray<SemanticFact> Facts(SemanticExecutionPlan plan, IReadOnlyList<SeedGroup> seeds) =>
    [
        .. seeds.SelectMany(group => group.Events
            .Select(seeded => (Seeded: seeded, Contract: plan.Events.Values.FirstOrDefault(contract => contract.Name == seeded.Event)))
            .Where(entry => entry.Contract is not null)
            .Select(entry => SemanticWorldRebuilder.SeedFact(plan, entry.Contract!, group.EventSourceId, entry.Seeded.Content)))
    ];

    internal static async Task Append(IChronicleServicesAccessor accessor, string eventStore, SemanticExecutionPlan plan, IReadOnlyList<SeedGroup> seeds)
    {
        foreach (var group in seeds)
        {
            var events = group.Events
                .Select(seeded => (Seeded: seeded, Contract: plan.Events.Values.FirstOrDefault(contract => contract.Name == seeded.Event)))
                .Where(entry => entry.Contract is not null)
                .ToArray();
            if (events.Length == 0)
            {
                continue;
            }

            var response = await accessor.Services.Sequences.AppendMany(new ChronicleSequences.AppendManyRequest
            {
                EventStore = eventStore,
                Namespace = EventStoreNamespaceName.Default,
                EventSequenceId = EventSequenceId.Log,
                EventSourceId = group.EventSourceId,
                CausedBy = new ChronicleSequences.Identity { Subject = Seeder, Name = Seeder, UserName = Seeder },
                Events =
                [
                    .. events.Select(entry => new ChronicleSequences.EventToAppend
                    {
                        EventType = new ChronicleSequences.EventType { Id = SemanticStoredEventNames.Of(entry.Contract!), Generation = 1 },
                        Content = entry.Seeded.Content.ToJsonString(),
                    })
                ],
            });
            response.EnsureSuccess();
            var rejection = Rejection(response.Response);
            if (rejection is not null)
            {
                throw new InvalidOperationException($"Seeding '{group.EventSourceId}' was rejected: {rejection}");
            }
        }
    }

    static string? Rejection(ChronicleSequences.AppendManyResponse response)
    {
        var reasons = response.ConstraintViolations.Select(violation => $"{violation.ConstraintName}: {violation.Message}")
            .Concat(response.Errors)
            .Concat(response.ConcurrencyViolations.Select(_ => "concurrency violation"))
            .ToArray();
        return reasons.Length > 0 ? string.Join("; ", reasons) : null;
    }

    static JsonObject Content(IEnumerable<PropertyMappingSyntax> properties)
    {
        var content = new JsonObject();
        foreach (var property in properties)
        {
            content[property.Property] = Value(property.Source);
        }

        return content;
    }

    static JsonNode? Value(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax { Value: null } => null,
        LiteralExpressionSyntax { Value: string text } => JsonValue.Create(text),
        LiteralExpressionSyntax { Value: bool flag } => JsonValue.Create(flag),
        LiteralExpressionSyntax { Value: ExactNumber exact } => Number(exact.Value),
        LiteralExpressionSyntax { Value: int or long or double or decimal or float } literal => Number(Convert.ToDecimal(literal.Value, CultureInfo.InvariantCulture)),
        ListExpressionSyntax list => new JsonArray([.. list.Items.Select(Value)]),
        ObjectExpressionSyntax @object => new JsonObject(@object.Members.Select(member => KeyValuePair.Create(member.Name, Value(member.Value)))),
        _ => JsonValue.Create(expression.ToString()),
    };

    // A whole number stays a whole number: a decimal 49 would append as 49.0 and miss an integer property's schema.
    static JsonValue Number(decimal value) => value == decimal.Truncate(value) && Math.Abs(value) < long.MaxValue ? JsonValue.Create((long)value) : JsonValue.Create(value);
}
