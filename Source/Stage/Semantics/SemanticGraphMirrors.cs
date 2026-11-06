// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;
using ChronicleSinks = Cratis.Chronicle.Contracts.Sinks;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Lowers the graphs and graph projections of a semantic application to the Chronicle contracts the kernel registers,
/// so the kernel's graph sink projects them whichever kernel Stage runs against.
/// </summary>
/// <remarks>
/// The semantic engine decides commands, read models and queries in process, so it registers only event types with the
/// kernel. A graph has no in-process reader yet: the kernel's sink is the only place it is projected, which is why this
/// registration is what makes a graph exist at all. The definition is the one Chronicle's own client builds for a
/// <c language="csharp">[Graph]</c> model, so a graph registered here is indistinguishable from one a rendered application registers.
/// </remarks>
public static class SemanticGraphMirrors
{
    /// <summary>The sink type of the Neo4j graph sink, which is the default graph sink of Chronicle's clients.</summary>
    public const string Neo4jSinkTypeId = "Neo4j";

    const uint FirstGeneration = 1;
    const string EventSourceIdExpression = "$eventSourceId";

    /// <summary>
    /// Lowers every graph of the application to a read-model and projection definition pair.
    /// </summary>
    /// <param name="plan">The execution plan of the model being run.</param>
    /// <param name="sinkTypeId">The graph sink type the kernel projects to.</param>
    /// <returns>One read model and projection per graph; empty when the application declares none.</returns>
    /// <exception cref="UnsupportedSemanticGraph">A construct has no equivalent in the kernel's graph projection.</exception>
    public static IReadOnlyList<(ChronicleReadModels.ReadModelDefinition ReadModel, ChronicleProjections.ProjectionDefinition Projection)> Lower(SemanticExecutionPlan plan, string sinkTypeId = Neo4jSinkTypeId)
    {
        var application = plan.Model.Application;
        var result = new List<(ChronicleReadModels.ReadModelDefinition, ChronicleProjections.ProjectionDefinition)>();
        foreach (var graph in application.Graphs.IsDefault ? [] : application.Graphs)
        {
            var projections = (application.GraphProjections.IsDefault ? [] : application.GraphProjections).Where(candidate => candidate.Graph == graph.Name).ToArray();
            var projectionId = Runtime.StageChronicleDefinitions.DeterministicGuid($"{graph.Name}:graph-projection").ToString();
            result.Add((ReadModel(graph, application, projectionId, sinkTypeId), Projection(plan, graph, projections, projectionId)));
        }

        return result;
    }

    static ChronicleReadModels.ReadModelDefinition ReadModel(SemanticGraph graph, SemanticApplication application, string projectionId, string sinkTypeId) =>
        new()
        {
            Type = new() { Identifier = graph.Name, Generation = FirstGeneration },
            ContainerName = graph.Name,
            DisplayName = graph.Name,
            Sink = new ChronicleSinks.SinkDefinition { ConfigurationId = Guid.Empty, TypeId = sinkTypeId },
            Schema = Schema(graph, application),
            Indexes = [],
            ObserverType = ChronicleReadModels.ReadModelObserverType.Projection,
            ObserverIdentifier = projectionId,
            Owner = ChronicleReadModels.ReadModelOwner.Client,
            Source = ChronicleReadModels.ReadModelSource.User,
        };

    // The schema is the one Chronicle's GraphSchemaWriter writes: one object per node label and edge type, marked with the
    // x-graph-* keywords the sink reads to find the keys and the end nodes.
    static string Schema(SemanticGraph graph, SemanticApplication application)
    {
        var properties = new JsonObject();
        foreach (var node in graph.Nodes)
        {
            var element = Element(node.Properties, application);
            element["x-graph-kind"] = "Node";
            element["x-graph-key"] = node.Properties.Single(property => property.IsKey).Name;
            properties[node.Label] = element;
        }

        foreach (var edge in graph.Edges)
        {
            var element = Element(edge.Properties, application);
            element["x-graph-kind"] = "Edge";
            element["x-graph-source"] = edge.Source;
            element["x-graph-target"] = edge.Target;
            if (edge.IdentifiedBy is not null)
            {
                element["x-graph-identity"] = edge.IdentifiedBy;
            }

            properties[edge.Type] = element;
        }

        return new JsonObject { ["type"] = "object", ["title"] = graph.Name, ["x-graph"] = true, ["properties"] = properties }.ToJsonString();
    }

    static JsonObject Element(IEnumerable<SemanticGraphProperty> properties, SemanticApplication application)
    {
        var nodes = new JsonObject();
        foreach (var property in properties)
        {
            nodes[property.Name] = SemanticSchemas.TypeSchemaOf(property.Type, application);
        }

        return new JsonObject { ["type"] = "object", ["properties"] = nodes };
    }

    static ChronicleProjections.ProjectionDefinition Projection(SemanticExecutionPlan plan, SemanticGraph graph, SemanticGraphProjection[] projections, string projectionId)
    {
        var nodes = new Dictionary<string, ChronicleProjections.GraphNodeProjectionDefinition>(StringComparer.Ordinal);
        var edges = new Dictionary<string, ChronicleProjections.GraphEdgeProjectionDefinition>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            nodes[node.Label] = new();
        }

        foreach (var edge in graph.Edges)
        {
            edges[edge.Type] = new();
        }

        foreach (var projection in projections)
        {
            foreach (var element in projection.Nodes)
            {
                var target = nodes[element.Name];
                foreach (var handler in element.Handlers)
                {
                    var @event = EventTypeOf(plan, handler.Event);
                    if (handler.Removes)
                    {
                        target.RemovedWith[@event] = new() { Key = Expression(plan, handler, handler.Key), Detach = handler.Detach };
                    }
                    else
                    {
                        target.From[@event] = new() { Key = Expression(plan, handler, handler.Key), Properties = Properties(plan, handler, graph.Nodes.Single(node => node.Label == element.Name).Properties.Single(property => property.IsKey).Name) };
                    }
                }
            }

            foreach (var element in projection.Edges)
            {
                var target = edges[element.Name];
                foreach (var handler in element.Handlers)
                {
                    var @event = EventTypeOf(plan, handler.Event);
                    var source = Expression(plan, handler, handler.Source);
                    var end = Expression(plan, handler, handler.Target);
                    var identity = handler.Identity is null ? null : Expression(plan, handler, handler.Identity);
                    if (handler.Removes)
                    {
                        target.RemovedWith[@event] = new() { SourceKey = source, TargetKey = end, Identity = identity };
                    }
                    else
                    {
                        target.From[@event] = new() { SourceKey = source, TargetKey = end, Identity = identity, Properties = Properties(plan, handler, graph.Edges.Single(edge => edge.Type == element.Name).IdentifiedBy) };
                    }
                }
            }
        }

        return new()
        {
            EventSequenceId = Cratis.Chronicle.EventSequences.EventSequenceId.Log,
            Identifier = projectionId,
            ReadModel = graph.Name,
            IsActive = true,
            IsRewindable = true,
            InitialModelState = "{}",
            All = new ChronicleProjections.FromEveryDefinition(),
            Graph = new() { Nodes = nodes, Edges = edges },
        };
    }

    static string EventTypeOf(SemanticExecutionPlan plan, SemanticId id) =>
        plan.Events.TryGetValue(id, out var contract)
            ? $"{SemanticStoredEventNames.Of(contract)}+{FirstGeneration}"
            : throw new UnsupportedSemanticGraph("A graph projection reacts to an event the execution plan does not admit.");

    // The key of a node and the identity of an edge are not properties: the kernel takes them from the key expressions and refuses a mapping onto them.
    static Dictionary<string, string> Properties(SemanticExecutionPlan plan, SemanticGraphHandler handler, string? identifying)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in handler.Mappings.Where(mapping => mapping.Property != identifying))
        {
            if (mapping.Operation != SemanticProjectionOperation.Set || mapping.Source is null || mapping.Factor is not null)
            {
                throw new UnsupportedSemanticGraph($"Graph property '{mapping.Property}' uses '{mapping.Operation}', which Stage does not lower to the kernel's graph projection yet; only plain assignment is carried over.");
            }

            result[mapping.Property] = Expression(plan, handler, mapping.Source);
        }

        return result;
    }

    static string Expression(SemanticExecutionPlan plan, SemanticGraphHandler handler, SemanticProjectionValue? value) =>
        value switch
        {
            null or SemanticProjectionEventSourceIdentity => EventSourceIdExpression,
            SemanticProjectionEventContextValue context => $"$eventContext({context.Path})",
            SemanticProjectionEventProperty property => string.Join('.', property.Path.Select(step => PropertyName(plan, handler, step))),
            _ => throw new UnsupportedSemanticGraph("A graph projection value has no equivalent in the kernel's graph projection.")
        };

    static string PropertyName(SemanticExecutionPlan plan, SemanticGraphHandler handler, SemanticId id) =>
        plan.Events[handler.Event].Properties.SingleOrDefault(property => property.Id == id)?.Name
            ?? throw new UnsupportedSemanticGraph("A graph projection reads an event property the event does not have.");
}

/// <summary>
/// Exception thrown when a graph construct cannot be lowered to the kernel's graph projection.
/// </summary>
/// <param name="message">What cannot be lowered.</param>
public sealed class UnsupportedSemanticGraph(string message) : Exception(message);
