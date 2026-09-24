// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Projections;

using ChronicleEvents = Cratis.Chronicle.Contracts.Events;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;
using ChronicleSinks = Cratis.Chronicle.Contracts.Sinks;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Translates the read models and projections modeled in an <see cref="EventModel"/> into the Chronicle contract
/// definitions the kernel registers at runtime, so that projections actually run and populate the read-model store —
/// without any compiled read-model type or attributes.
/// </summary>
public static class StageChronicleDefinitions
{
    const string EventSourceIdExpression = "$eventSourceId";
    const uint FirstGeneration = 1;

    // The in-memory projection sink registered by the Chronicle kernel. Sink types are identified by the well-known
    // names in Chronicle's WellKnownSinkTypes, not by a GUID — the kernel rejects an unknown id at the point a
    // projection tries to write, which surfaces as a failed observer partition rather than a registration error.
    // Read models are projected into the kernel's in-memory store so the whole Stage runs without any database.
    const string InMemorySinkTypeId = "InMemory";

    // The property a read model document is identified by, and the schema keyword that says so.
    const string IdentityPropertyName = "id";
    const string IdentityKeyword = "identity";

    /// <summary>
    /// Builds the Chronicle read-model and projection definitions for every read model in the model that has a
    /// projection.
    /// </summary>
    /// <param name="model">The event model being run.</param>
    /// <param name="eventSequenceId">The event sequence the projections read from (typically the event log).</param>
    /// <returns>The read-model and projection definitions to register, keyed to each other by identifier.</returns>
    public static (IList<ChronicleReadModels.ReadModelDefinition> ReadModels, IList<ChronicleProjections.ProjectionDefinition> Projections)
        Build(EventModel model, string eventSequenceId)
    {
        var readModels = new List<ChronicleReadModels.ReadModelDefinition>();
        var projections = new List<ChronicleProjections.ProjectionDefinition>();

        foreach (var located in StageModelWalker.Slices(model))
        {
            if (located.Slice.ReadModel is not { Projection: { } projection } readModel)
            {
                continue;
            }

            // The projection identifier must be DISTINCT from the read-model identifier — sharing them makes the
            // Chronicle ProjectionsManager grain hang. Derive a stable, distinct projection id from the read-model id.
            var readModelIdentifier = readModel.Id.ToString();
            var projectionIdentifier = DeterministicGuid($"{readModelIdentifier}:projection").ToString();

            readModels.Add(BuildReadModel(readModel, readModelIdentifier, projectionIdentifier));
            projections.Add(BuildProjection(projection, projectionIdentifier, readModelIdentifier, eventSequenceId));
        }

        return (readModels, projections);
    }

    /// <summary>
    /// Builds the Chronicle event type registrations for every event declared in the model. The event name is the
    /// event type identifier — the same identity the projections and the appended events use.
    /// </summary>
    /// <param name="model">The event model being run.</param>
    /// <returns>The event type registrations, one per distinct event name.</returns>
    /// <exception cref="UnsupportedProjectionEventType">A declared event requires a generation the Stage schema registration cannot provide.</exception>
    public static IList<ChronicleEvents.EventTypeRegistration> BuildEventTypes(EventModel model)
    {
        var events = StageModelWalker.Slices(model)
            .SelectMany(located => located.Slice.Events)
            .DistinctBy(@event => @event.Name, StringComparer.Ordinal)
            .ToArray();
        if (events.Any(@event => @event.Name.Contains('+', StringComparison.Ordinal)))
        {
            throw new UnsupportedProjectionEventType(events.First(@event => @event.Name.Contains('+', StringComparison.Ordinal)).Name);
        }

        return [.. events.Select(@event => new ChronicleEvents.EventTypeRegistration
        {
            Type = EventType(@event.Name),
            Schema = @event.Schema,
            Owner = ChronicleEvents.EventTypeOwner.Client,
            Source = ChronicleEvents.EventTypeSource.Code,
            Generations = [new ChronicleEvents.EventTypeGenerationDefinition { Generation = FirstGeneration, Schema = @event.Schema }],
        })];
    }

    internal static ChronicleReadModels.ReadModelDefinition BuildReadModel(ReadModelDefinition readModel, string readModelIdentifier, string projectionIdentifier) =>
        new()
        {
            Type = new() { Identifier = readModelIdentifier, Generation = FirstGeneration },
            ContainerName = ModelNaming.ToIdentifier(readModel.Name),
            DisplayName = readModel.Name,
            Sink = new ChronicleSinks.SinkDefinition { ConfigurationId = Guid.Empty, TypeId = InMemorySinkTypeId },
            Schema = WithIdentityProperty(readModel.Schema),
            Indexes = [],
            ObserverType = ChronicleReadModels.ReadModelObserverType.Projection,
            ObserverIdentifier = projectionIdentifier,
            Owner = ChronicleReadModels.ReadModelOwner.Client,
            Source = ChronicleReadModels.ReadModelSource.User,
        };

    internal static ChronicleProjections.ProjectionDefinition BuildProjection(ProjectionDefinition projection, string projectionIdentifier, string readModelIdentifier, string eventSequenceId) =>
        new()
        {
            EventSequenceId = eventSequenceId,
            Identifier = projectionIdentifier,
            ReadModel = readModelIdentifier,
            IsActive = projection.IsActive,
            IsRewindable = projection.IsRewindable,
            InitialModelState = projection.InitialModelState,
            From = FromMap(projection.From),
            Join = JoinMap(projection.Join),
            Children = ChildrenMap(projection.Children),
            FromEvery = [.. projection.FromDerivatives.Select(Derivative)],
            All = Every(projection.FromEvery),
            FromEventProperty = projection.FromEventProperty is { } fromEventProperty ? EventProperty(fromEventProperty) : null,
            RemovedWith = RemovedWithMap(projection.RemovedWith),
            RemovedWithJoin = RemovedWithJoinMap(projection.RemovedWithJoin),
            Tags = [.. projection.Tags],
            AutoMap = (ChronicleProjections.AutoMap)(int)projection.AutoMap,
            Nested = ChildrenMap(projection.Nested ?? new Dictionary<string, ChildrenDefinition>()),
            SubscribesToAllEvents = projection.SubscribesToAllEvents,
        };

    internal static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return new Guid(hash[..16]);
    }

    /// <summary>
    /// Ensures the read model's schema declares the property its documents are identified by.
    /// </summary>
    /// <param name="schema">The schema synthesized from the model.</param>
    /// <returns>The schema, with an identity property added when it declares none.</returns>
    /// <remarks>
    /// <para>
    /// Screenplay read models declare their shape, never their identity: the projection's key decides it. Chronicle
    /// writes that key into the document's identity property, and a schema that names none made the projection
    /// fail on its first event with "Value cannot be null. (Parameter 'key')" - the read model then stayed empty
    /// forever, with nothing in the play session saying why.
    /// </para>
    /// </remarks>
    static string WithIdentityProperty(string schema)
    {
        try
        {
            var document = System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(schema) ? "{}" : schema) as System.Text.Json.Nodes.JsonObject
                ?? [];

            if (document["properties"] is not System.Text.Json.Nodes.JsonObject properties)
            {
                properties = [];
                document["properties"] = properties;
            }

            document["type"] ??= "object";

            if (properties[IdentityPropertyName] is null)
            {
                properties[IdentityPropertyName] = new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "string",
                    [IdentityKeyword] = true
                };
            }

            return document.ToJsonString();
        }
        catch (System.Text.Json.JsonException)
        {
            return schema;
        }
    }

    static ChronicleEvents.EventType EventType(string name) =>
        name.Contains('+', StringComparison.Ordinal)
            ? ParseVersionedEventType(name)
            : new() { Id = name, Generation = FirstGeneration };

    static ChronicleEvents.EventType ParseVersionedEventType(string name)
    {
        var segments = name.Split('+');
        if (segments.Length is < 2 or > 3 || !uint.TryParse(
                segments[1],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var generation) ||
            (segments.Length == 3 && !bool.TryParse(segments[2], out _)))
        {
            throw new UnsupportedProjectionEventType(name);
        }

        return new ChronicleEvents.EventType
        {
            Id = segments[0],
            Generation = generation,
            Tombstone = segments.Length == 3 && bool.Parse(segments[2])
        };
    }

    static Dictionary<string, string> Properties(IReadOnlyList<PropertyMapping> mappings)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            properties[mapping.Property] = ProjectionRuntimeExpression.Translate(mapping.Expression);
        }

        return properties;
    }

    static ChronicleProjections.FromDefinition From(FromDefinition from) =>
        new()
        {
            Properties = Properties(from.Properties),
            Key = ProjectionRuntimeKey.Translate(from.Key ?? EventSourceIdExpression),
            ParentKey = from.ParentKey is null ? null : ProjectionRuntimeKey.Translate(from.ParentKey),
        };

    static ChronicleProjections.JoinDefinition Join(JoinDefinition join) =>
        new()
        {
            On = join.On,
            Properties = Properties(join.Properties),
            Key = ProjectionRuntimeKey.Translate(join.Key ?? EventSourceIdExpression),
        };

    static ChronicleProjections.FromEveryDefinition Every(FromEveryDefinition every) =>
        new()
        {
            Properties = Properties(every.Properties),
            IncludeChildren = every.IncludeChildren,
            AutoMap = (ChronicleProjections.AutoMap)(int)every.AutoMap,
        };

    static ChronicleProjections.FromDerivativesDefinition Derivative(FromDerivativesDefinition derivative) =>
        new()
        {
            EventTypes = [.. derivative.EventTypes.Select(EventType)],
            From = From(derivative.From),
        };

    static ChronicleProjections.FromEventPropertyDefinition EventProperty(FromEventPropertyDefinition fromEventProperty) =>
        new()
        {
            Event = EventType(fromEventProperty.EventType),
            PropertyExpression = ProjectionRuntimeExpression.Translate(fromEventProperty.Expression),
        };

    static ChronicleProjections.ChildrenDefinition Child(ChildrenDefinition children) =>
        new()
        {
            IdentifiedBy = ProjectionRuntimeKey.Identity(children.IdentifiedBy),
            From = FromMap(children.From),
            Join = JoinMap(children.Join),
            Children = ChildrenMap(children.Children),
            All = Every(children.All),
            FromEventProperty = children.FromEventProperty is { } fromEventProperty ? EventProperty(fromEventProperty) : null,
            RemovedWith = RemovedWithMap(children.RemovedWith),
            RemovedWithJoin = RemovedWithJoinMap(children.RemovedWithJoin),
            AutoMap = (ChronicleProjections.AutoMap)(int)children.AutoMap,
            Nested = ChildrenMap(children.Nested ?? new Dictionary<string, ChildrenDefinition>()),
        };

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.FromDefinition> FromMap(IReadOnlyDictionary<string, FromDefinition> source) =>
        EventMap(source, From);

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.JoinDefinition> JoinMap(IReadOnlyDictionary<string, JoinDefinition> source) =>
        EventMap(source, Join);

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.RemovedWithDefinition> RemovedWithMap(IReadOnlyDictionary<string, RemovedWithDefinition> source) =>
        EventMap(source, removal => new ChronicleProjections.RemovedWithDefinition
        {
            Key = ProjectionRuntimeKey.Translate(removal.Key),
            ParentKey = removal.ParentKey is null ? null : ProjectionRuntimeKey.Translate(removal.ParentKey)
        });

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.RemovedWithJoinDefinition> RemovedWithJoinMap(IReadOnlyDictionary<string, RemovedWithJoinDefinition> source) =>
        EventMap(source, removal => new ChronicleProjections.RemovedWithJoinDefinition { Key = ProjectionRuntimeKey.Translate(removal.Key) });

    static Dictionary<ChronicleEvents.EventType, TResult> EventMap<TSource, TResult>(IReadOnlyDictionary<string, TSource> source, Func<TSource, TResult> convert)
    {
        var mapped = new Dictionary<ChronicleEvents.EventType, TResult>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, value) in source)
        {
            var type = EventType(name);
            var identity = $"{type.Id}+{type.Generation}+{type.Tombstone}";
            if (!seen.Add(identity))
            {
                throw new UnsupportedProjectionEventType($"Duplicate projection event identity for '{name}'.");
            }

            mapped.Add(type, convert(value));
        }

        return mapped;
    }

    static Dictionary<string, ChronicleProjections.ChildrenDefinition> ChildrenMap(IReadOnlyDictionary<string, ChildrenDefinition> source) =>
        source.ToDictionary(entry => entry.Key, entry => Child(entry.Value));
}
