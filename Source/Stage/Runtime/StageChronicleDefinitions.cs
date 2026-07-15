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
    const string MongoSinkTypeId = "22202c41-2be1-4547-9c00-f0b1f797fd75";

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

    static ChronicleReadModels.ReadModelDefinition BuildReadModel(ReadModelDefinition readModel, string readModelIdentifier, string projectionIdentifier) =>
        new()
        {
            Type = new() { Identifier = readModelIdentifier, Generation = FirstGeneration },
            ContainerName = ModelNaming.ToIdentifier(readModel.Name),
            DisplayName = readModel.Name,
            Sink = new ChronicleSinks.SinkDefinition { ConfigurationId = Guid.Empty, TypeId = MongoSinkTypeId },
            Schema = readModel.Schema,
            Indexes = [],
            ObserverType = ChronicleReadModels.ReadModelObserverType.Projection,
            ObserverIdentifier = projectionIdentifier,
            Owner = ChronicleReadModels.ReadModelOwner.Client,
            Source = ChronicleReadModels.ReadModelSource.User,
        };

    static ChronicleProjections.ProjectionDefinition BuildProjection(ProjectionDefinition projection, string projectionIdentifier, string readModelIdentifier, string eventSequenceId) =>
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
            Nested = new Dictionary<string, ChronicleProjections.ChildrenDefinition>(),
            SubscribesToAllEvents = false,
        };

    static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return new Guid(hash[..16]);
    }

    static ChronicleEvents.EventType EventType(string name) =>
        new() { Id = name, Generation = FirstGeneration };

    static Dictionary<string, string> Properties(IReadOnlyList<PropertyMapping> mappings) =>
        mappings.ToDictionary(mapping => mapping.Property, mapping => mapping.Expression);

    static ChronicleProjections.FromDefinition From(FromDefinition from) =>
        new()
        {
            Properties = Properties(from.Properties),
            Key = from.Key ?? EventSourceIdExpression,
            ParentKey = from.ParentKey,
        };

    static ChronicleProjections.JoinDefinition Join(JoinDefinition join) =>
        new()
        {
            On = join.On,
            Properties = Properties(join.Properties),
            Key = join.Key ?? EventSourceIdExpression,
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
            PropertyExpression = fromEventProperty.Expression,
        };

    static ChronicleProjections.ChildrenDefinition Child(ChildrenDefinition children) =>
        new()
        {
            IdentifiedBy = children.IdentifiedBy,
            From = FromMap(children.From),
            Join = JoinMap(children.Join),
            Children = ChildrenMap(children.Children),
            All = Every(children.All),
            FromEventProperty = children.FromEventProperty is { } fromEventProperty ? EventProperty(fromEventProperty) : null,
            RemovedWith = RemovedWithMap(children.RemovedWith),
            RemovedWithJoin = RemovedWithJoinMap(children.RemovedWithJoin),
            AutoMap = (ChronicleProjections.AutoMap)(int)children.AutoMap,
        };

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.FromDefinition> FromMap(IReadOnlyDictionary<string, FromDefinition> source) =>
        source.ToDictionary(entry => EventType(entry.Key), entry => From(entry.Value));

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.JoinDefinition> JoinMap(IReadOnlyDictionary<string, JoinDefinition> source) =>
        source.ToDictionary(entry => EventType(entry.Key), entry => Join(entry.Value));

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.RemovedWithDefinition> RemovedWithMap(IReadOnlyDictionary<string, RemovedWithDefinition> source) =>
        source.ToDictionary(entry => EventType(entry.Key), entry => new ChronicleProjections.RemovedWithDefinition { Key = entry.Value.Key, ParentKey = entry.Value.ParentKey });

    static Dictionary<ChronicleEvents.EventType, ChronicleProjections.RemovedWithJoinDefinition> RemovedWithJoinMap(IReadOnlyDictionary<string, RemovedWithJoinDefinition> source) =>
        source.ToDictionary(entry => EventType(entry.Key), entry => new ChronicleProjections.RemovedWithJoinDefinition { Key = entry.Value.Key });

    static Dictionary<string, ChronicleProjections.ChildrenDefinition> ChildrenMap(IReadOnlyDictionary<string, ChildrenDefinition> source) =>
        source.ToDictionary(entry => entry.Key, entry => Child(entry.Value));
}
