// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using ChronicleEventTypes = Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Stage.Host;

/// <summary>What the kernel holds for the first generation of one event type.</summary>
internal enum SemanticStoredSchemaState
{
    /// <summary>The query succeeded and the kernel holds no generation of the type.</summary>
    NotRegistered,

    /// <summary>The kernel holds exactly one first generation, whose schema was read.</summary>
    Stored,

    /// <summary>The query failed or its answer cannot be trusted; nothing may be inferred from it.</summary>
    Unknown
}

/// <summary>The stored first-generation schema of one event type.</summary>
/// <param name="State">What is known about the stored schema.</param>
/// <param name="Schema">The schema, when <see cref="State"/> is <see cref="SemanticStoredSchemaState.Stored"/>.</param>
internal sealed record SemanticStoredSchema(SemanticStoredSchemaState State, JsonObject? Schema = null)
{
    internal static readonly SemanticStoredSchema Unknown = new(SemanticStoredSchemaState.Unknown);

    internal static readonly SemanticStoredSchema NotRegistered = new(SemanticStoredSchemaState.NotRegistered);

    /// <summary>Gets whether the stored schema is known and every number in it carries the exact decimal format.</summary>
    internal bool IsExactDecimal => State == SemanticStoredSchemaState.Stored && SemanticSchemas.HasOnlyExactNumbers(Schema!);
}

/// <summary>
/// Reads the stored first-generation schemas of a model's event types, and decides from them what a restart can read back exactly.
/// </summary>
/// <remarks>
/// A number without the exact decimal format is converted to a double when an event is appended, so the loss cannot be recovered from storage.
/// Whether a restart may trust a decimal is therefore decided from the stored schema of the event type, never from the values read back.
/// </remarks>
internal static class SemanticStoredSchemas
{
    internal const string LegacyDecimal = "STAGE-ESM-079";
    internal const string DateTimeProperty = "STAGE-ESM-080";

    /// <summary>
    /// Reads the stored first-generation schema of each event type of the plan, by stored event name.
    /// </summary>
    /// <param name="accessor">The Chronicle services.</param>
    /// <param name="store">The event store name.</param>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The stored schema of each event type; a failed query is <see cref="SemanticStoredSchema.Unknown"/>, never not registered.</returns>
    internal static async Task<IReadOnlyDictionary<string, SemanticStoredSchema>> Read(IChronicleServicesAccessor accessor, string store, SemanticExecutionPlan plan)
    {
        var names = plan.Events.Values.Select(SemanticStoredEventNames.Of).Distinct().ToArray();
        var schemas = await Task.WhenAll(names.Select(name => Read(accessor, store, name)));
        return names.Zip(schemas).ToDictionary(pair => pair.First, pair => pair.Second);
    }

    /// <summary>
    /// Gets the schema to register for an event type: exact decimals for a type the kernel does not hold, the stored shape for one it does, and
    /// the default shape when the stored one is not known or the event type belongs to another store.
    /// </summary>
    /// <param name="event">The event contract.</param>
    /// <param name="application">The application the contract belongs to.</param>
    /// <param name="stored">The stored schema of the event type.</param>
    /// <returns>The schema in JSON form.</returns>
    internal static string Schema(SemanticEventContract @event, SemanticApplication application, SemanticStoredSchema stored)
    {
        var built = SemanticSchemas.Schema(@event.Properties, application);
        if (SemanticPublicEvents.IsForeign(@event)) return built;

        return stored.State switch
        {
            SemanticStoredSchemaState.NotRegistered => SemanticSchemas.WithDecimalFormats(JsonNode.Parse(built)!.AsObject(), null).ToJsonString(),
            SemanticStoredSchemaState.Stored => SemanticSchemas.WithDecimalFormats(JsonNode.Parse(built)!.AsObject(), stored.Schema).ToJsonString(),
            _ => built
        };
    }

    /// <summary>
    /// Gets the stored event names whose stored schema carries the exact decimal format on every number.
    /// </summary>
    /// <param name="stored">The stored schemas, read after the contracts were registered.</param>
    /// <returns>The names a restart may read decimals back from exactly.</returns>
    internal static IReadOnlySet<string> ExactDecimals(IReadOnlyDictionary<string, SemanticStoredSchema> stored) =>
        stored.Where(entry => entry.Value.IsExactDecimal).Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Gets the warnings for event types a restart will refuse, or that Chronicle cannot read back exactly, before they are registered.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="stored">The stored schemas, read before the contracts were registered.</param>
    /// <returns>The warnings.</returns>
    internal static IEnumerable<SemanticModelLoadDiagnostic> Warnings(SemanticExecutionPlan plan, IReadOnlyDictionary<string, SemanticStoredSchema> stored)
    {
        foreach (var @event in plan.Events.Values)
        {
            var state = stored[SemanticStoredEventNames.Of(@event)];
            if (Uses(@event, plan.Model.Application, SemanticPrimitiveType.DecimalNumber) &&
                (state.State != SemanticStoredSchemaState.NotRegistered || SemanticPublicEvents.IsForeign(@event)) &&
                !state.IsExactDecimal)
            {
                yield return Warning(LegacyDecimal, $"Event '{@event.Name}' has a decimal property, but its stored schema does not keep decimals exactly. The application starts, and a restart will refuse this event log. Use a new event type to get exact decimals.");
            }

            if (Uses(@event, plan.Model.Application, SemanticPrimitiveType.DateTime))
            {
                yield return Warning(DateTimeProperty, $"Event '{@event.Name}' has a date and time property, which Chronicle storage cannot read back exactly. The application starts, and a restart will refuse this event log once an event of this type is stored.");
            }
        }
    }

    static async Task<SemanticStoredSchema> Read(IChronicleServicesAccessor accessor, string store, string name)
    {
        try
        {
            var result = await accessor.Services.EventTypes.AllEventTypeGenerations(new ChronicleEventTypes.AllEventTypeGenerationsRequest { EventStore = store, EventTypeId = name });
            if (!result.IsSuccess) return SemanticStoredSchema.Unknown;

            var generations = result.Data.ToArray();
            if (generations.Length == 0) return SemanticStoredSchema.NotRegistered;

            var first = generations.Where(generation => generation.Type.Generation == 1).ToArray();
            return first.Length == 1 && JsonNode.Parse(first[0].Schema) is JsonObject schema
                ? new SemanticStoredSchema(SemanticStoredSchemaState.Stored, schema)
                : SemanticStoredSchema.Unknown;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return SemanticStoredSchema.Unknown;
        }
    }

    static bool Uses(SemanticEventContract @event, SemanticApplication application, SemanticPrimitiveType primitive) =>
        @event.Properties.Any(property => Uses(property.Type, application, primitive));

    static bool Uses(SemanticTypeReference type, SemanticApplication application, SemanticPrimitiveType primitive)
    {
        if (type.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            return application.Types.Single(candidate => candidate.Id == type.Target).Properties.Any(property => Uses(property.Type, application, primitive));
        }

        return (type.Kind == SemanticTypeReferenceKind.Concept ? application.Concepts.Single(candidate => candidate.Id == type.Target).Primitive : type.Primitive) == primitive;
    }

    static SemanticModelLoadDiagnostic Warning(string code, string message) => new(code, message) { Severity = DiagnosticSeverity.Warning };
}
