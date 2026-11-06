// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Refuses, with a named diagnostic, every graph construct the Cratis renderer cannot express, and the whole of a graph while the
/// Chronicle client Stage pins does not carry the graph API. A graph construct is never dropped silently.
/// </summary>
internal static class SemanticGraphAdmission
{
    /// <summary>
    /// The code for a graph construct the renderer cannot express with Chronicle's model-bound graph API.
    /// </summary>
    internal const string Unsupported = "STAGE-ESM-062";

    /// <summary>
    /// The code for a graph specification the generated <c language="csharp">GraphScenario</c> cannot express.
    /// </summary>
    internal const string Specification = "STAGE-ESM-064";

    /// <summary>
    /// The code that says a scoped plan leaves the graphs of the application out.
    /// </summary>
    internal const string ScopedOut = "STAGE-ESM-066";

    /// <summary>
    /// Gets whether the application declares a graph, a graph projection or a graph specification.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>True when any graph construct is declared.</returns>
    internal static bool Declares(SemanticApplication application) =>
        !application.Graphs.IsDefaultOrEmpty || !application.GraphProjections.IsDefaultOrEmpty || !application.GraphSpecifications.IsDefaultOrEmpty;

    /// <summary>
    /// Checks what the application declares about graphs.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="chronicleAdmitsGraphs">Whether the pinned Chronicle client carries the graph API.</param>
    /// <returns>One error per refused construct; the gate error alone while the Chronicle client has no graph API.</returns>
    internal static ImmutableArray<ArtifactRenderDiagnostic> Check(SemanticApplicationContext context, bool chronicleAdmitsGraphs)
    {
        var application = context.Application;
        if (!Declares(application)) return [];
        if (!chronicleAdmitsGraphs)
        {
            var message = $"The application declares {Described(application)}, which Stage renders with Chronicle's graph projection API (Cratis/Chronicle#5680). The Chronicle client Stage pins ({Scaffolding.ScaffoldPackageVersions.Chronicle}) does not carry it yet, so the graph is refused rather than rendered without it.";
            return [Error(ChronicleGraphSupport.NeedsChronicleGraphs, context, message)];
        }

        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var checker = new Checker(context, diagnostics);
        checker.Graphs();
        checker.Projections();
        checker.Specifications();
        return [.. diagnostics];
    }

    /// <summary>
    /// Gets whether a state-view slice only hosts graph constructs: the application declares a graph, and the slice builds no read model
    /// and answers no query. The graph itself is application-level, so such a slice has nothing of its own to render as a read model.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="slice">The slice.</param>
    /// <returns>True when the slice is a state view that hosts graph constructs and nothing else.</returns>
    internal static bool HostsOnlyGraphs(SemanticApplication application, SemanticSlice slice) =>
        slice.Kind == SemanticSliceKind.StateView && !application.Graphs.IsDefaultOrEmpty &&
        slice.ReadModels.IsDefaultOrEmpty && slice.Reducers.IsDefaultOrEmpty && slice.Projections.IsDefaultOrEmpty && slice.Queries.IsDefaultOrEmpty;

    /// <summary>
    /// Gets whether a graph specification seeds a graph with <c language="text">given graph</c>, which the generated <c language="csharp">GraphScenario</c>
    /// cannot express: it builds a graph only from appended events.
    /// </summary>
    /// <param name="specification">The graph specification.</param>
    /// <returns>True when the specification seeds a graph.</returns>
    internal static bool IsSeeded(SemanticGraphSpecification specification) => !specification.GivenGraphs.IsDefaultOrEmpty;

    /// <summary>
    /// Names each graph specification the renderer leaves out and why, as a degradation: a seeded graph is a precondition the generated
    /// scenario cannot establish, and a specification run against an empty graph would assert something else.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>One degradation per omitted graph specification.</returns>
    internal static ImmutableArray<ArtifactRenderDegradation> Omitted(SemanticApplicationContext context) =>
        [.. context.Application.GraphSpecifications.Where(IsSeeded).Select(specification => new ArtifactRenderDegradation(
            new ArtifactRenderDiagnostic(
                ArtifactRenderDegradationCodes.UnsupportedConstruct,
                ArtifactRenderDiagnosticSeverity.Warning,
                $"{Specification}: graph specification '{specification.Name}' of slice '{specification.Owner}' seeds a graph with 'given graph' and is not rendered; the generated GraphScenario builds a graph only from appended events.",
                context.Application.Id),
            "Express the precondition as the events that build it, or verify the specification with the Screenplay reference evaluator."))];

    static string Described(SemanticApplication application) =>
        string.Join(", ", new[]
        {
            application.Graphs.IsDefaultOrEmpty ? null : $"{application.Graphs.Length} graph(s)",
            application.GraphProjections.IsDefaultOrEmpty ? null : $"{application.GraphProjections.Length} graph projection(s)",
            application.GraphSpecifications.IsDefaultOrEmpty ? null : $"{application.GraphSpecifications.Length} graph specification(s)"
        }.OfType<string>());

    static bool IsAttributeConstant(SemanticTypeReference type, SemanticValue value) =>
        type is { Kind: SemanticTypeReferenceKind.Primitive, IsCollection: false } && (type.Primitive, value) is
            (SemanticPrimitiveType.Text, SemanticTextValue) or
            (SemanticPrimitiveType.Boolean, SemanticBooleanValue) or
            (SemanticPrimitiveType.WholeNumber, SemanticNumberValue);

    static bool UsesEventSource(SemanticGraphHandler handler) =>
        handler.Key is SemanticProjectionEventSourceIdentity || handler.Source is SemanticProjectionEventSourceIdentity ||
        handler.Target is SemanticProjectionEventSourceIdentity || handler.Identity is SemanticProjectionEventSourceIdentity ||
        handler.Mappings.Any(mapping => mapping.Source is SemanticProjectionEventSourceIdentity);

    static ArtifactRenderDiagnostic Error(string code, SemanticApplicationContext context, string message) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, context.Application.Id);

    sealed class Checker(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        readonly Dictionary<string, SemanticGraph> _graphs = context.Application.Graphs.ToDictionary(graph => graph.Name, StringComparer.Ordinal);

        public void Graphs()
        {
            foreach (var graph in context.Application.Graphs)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in graph.Nodes)
                {
                    if (!SemanticGraphNaming.NodeLabelIsRecordName(node))
                    {
                        Refuse($"Node label '{node.Label}' of graph '{graph.Name}' cannot be the name of its record: Chronicle's graph API labels a node with the name of its type, so the label must be a PascalCase identifier.");
                    }

                    if (!names.Add(SemanticGraphNaming.NodeRecord(node))) Refuse($"Graph '{graph.Name}' renders two elements to the record '{node.Label}'.");
                    CheckProperties(graph, node.Label, node.Properties);
                }

                foreach (var edge in graph.Edges)
                {
                    if (!names.Add(SemanticGraphNaming.EdgeRecord(edge))) Refuse($"Edge type '{edge.Type}' of graph '{graph.Name}' renders to the record '{SemanticGraphNaming.EdgeRecord(edge)}', which another element of the graph already uses.");
                    CheckProperties(graph, edge.Type, edge.Properties);
                    if (edge.IdentifiedBy is { } identity && !edge.Properties.Any(property => string.Equals(property.Name, identity, StringComparison.Ordinal)))
                    {
                        Refuse($"Edge type '{edge.Type}' of graph '{graph.Name}' is identified by '{identity}', which it does not declare.");
                    }
                }
            }
        }

        public void Projections()
        {
            foreach (var projection in context.Application.GraphProjections)
            {
                if (!_graphs.TryGetValue(projection.Graph, out var graph))
                {
                    Refuse($"Graph projection '{projection.Name}' builds graph '{projection.Graph}', which is not declared.");
                    continue;
                }

                foreach (var element in projection.Nodes)
                {
                    var node = graph.Nodes.FirstOrDefault(candidate => string.Equals(candidate.Label, element.Name, StringComparison.Ordinal));
                    if (node is null) Refuse($"Graph projection '{projection.Name}' handles node '{element.Name}', which graph '{graph.Name}' does not declare.");
                    else CheckNodeHandlers(projection, node, element);
                }

                foreach (var element in projection.Edges)
                {
                    var edge = graph.Edges.FirstOrDefault(candidate => string.Equals(candidate.Type, element.Name, StringComparison.Ordinal));
                    if (edge is null) Refuse($"Graph projection '{projection.Name}' handles edge '{element.Name}', which graph '{graph.Name}' does not declare.");
                    else CheckEdgeHandlers(projection, edge, element);
                }
            }

            CheckSharedElements();
        }

        public void Specifications()
        {
            foreach (var specification in context.Application.GraphSpecifications)
            {
                var label = $"Graph specification '{specification.Name}' of slice '{specification.Owner}'";
                if (IsSeeded(specification)) continue;

                if (specification.WhenAppended is null && specification.Given.IsEmpty)
                {
                    Diagnostic(Specification, $"{label} appends no event, so there is nothing for the generated GraphScenario to project.");
                }

                foreach (var fact in specification.Given.Concat(specification.WhenAppended is null ? [] : [specification.WhenAppended]))
                {
                    CheckFact(label, fact);
                }

                foreach (var step in specification.ThenGraphs) CheckStep(label, step);
                if (context.AllSlices.All(located => !string.Equals(located.Slice.Name, specification.Owner, StringComparison.Ordinal)))
                {
                    Diagnostic(Specification, $"{label} belongs to a slice the application does not declare.");
                }
            }
        }

        void CheckFact(string label, SemanticSpecificationEvent fact)
        {
            if (!context.Events.TryGetValue(fact.EventContract, out var contract))
            {
                Diagnostic(Specification, $"{label} appends an event the application does not declare.");
                return;
            }

            if (contract.Properties.Any(property => fact.Values.All(value => value.TargetProperty != property.Id)))
            {
                Diagnostic(Specification, $"{label} appends '{contract.Name}' without stating every property of the event.");
            }

            var keyedByEventSource = context.Application.GraphProjections.Any(projection => projection.Nodes.Concat(projection.Edges)
                .SelectMany(element => element.Handlers).Any(handler => handler.Event == fact.EventContract && SemanticGraphAdmission.UsesEventSource(handler)));
            if (fact.EventSource is null && keyedByEventSource)
            {
                Diagnostic(Specification, $"{label} appends '{contract.Name}', which a graph projection keys on the event source, without stating the event source.");
            }
        }

        void CheckStep(string label, SemanticGraphStep step)
        {
            if (!_graphs.TryGetValue(step.Graph, out var graph))
            {
                Diagnostic(Specification, $"{label} asserts graph '{step.Graph}', which is not declared.");
                return;
            }

            foreach (var element in step.Elements)
            {
                var properties = element.IsNode
                    ? graph.Nodes.FirstOrDefault(node => string.Equals(node.Label, element.Name, StringComparison.Ordinal))?.Properties
                    : graph.Edges.FirstOrDefault(edge => string.Equals(edge.Type, element.Name, StringComparison.Ordinal))?.Properties;
                if (properties is null)
                {
                    Diagnostic(Specification, $"{label} asserts {(element.IsNode ? "node" : "edge")} '{element.Name}', which graph '{step.Graph}' does not declare.");
                    continue;
                }

                if (element.Values.Any(value => !properties.Value.Any(property => string.Equals(property.Name, value.Property, StringComparison.Ordinal))))
                {
                    Diagnostic(Specification, $"{label} states a property that {(element.IsNode ? "node" : "edge")} '{element.Name}' does not declare.");
                }
            }
        }

        void CheckProperties(SemanticGraph graph, string element, ImmutableArray<SemanticGraphProperty> properties)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in properties)
            {
                var name = SemanticGraphNaming.Property(property.Name);
                if (!SemanticGraphNaming.IsIdentifier(name) || !names.Add(name))
                {
                    Refuse($"Property '{property.Name}' of '{element}' in graph '{graph.Name}' does not render to a unique C# member.");
                }

                if (string.Equals(name, SemanticGraphNaming.EndpointSource, StringComparison.Ordinal) || string.Equals(name, SemanticGraphNaming.EndpointTarget, StringComparison.Ordinal))
                {
                    Refuse($"Property '{property.Name}' of '{element}' in graph '{graph.Name}' is named like an endpoint member of an edge record.");
                }
            }
        }

        void CheckNodeHandlers(SemanticGraphProjection projection, SemanticGraphNode node, SemanticGraphElementHandlers element)
        {
            var seen = new HashSet<SemanticId>();
            foreach (var handler in element.Handlers)
            {
                var label = $"Graph projection '{projection.Name}', node '{node.Label}'";
                if (!context.Events.TryGetValue(handler.Event, out var contract))
                {
                    Refuse($"{label}: the handled event is not declared.");
                    continue;
                }

                if (!seen.Add(handler.Event) && !handler.Removes) Refuse($"{label}: event '{contract.Name}' is handled more than once; Chronicle's model-bound graph API maps one handler per event.");
                CheckValue(label, "key", handler.Key, allowContext: false);
                if (handler.Removes && handler.Key is SemanticProjectionLiteral)
                {
                    Refuse($"{label}: a node is removed by an event property or the event source, not by a literal key.");
                }

                if (handler.Removes) continue;
                CheckMappings(label, node.Properties, handler);
            }
        }

        void CheckEdgeHandlers(SemanticGraphProjection projection, SemanticGraphEdge edge, SemanticGraphElementHandlers element)
        {
            var seen = new HashSet<SemanticId>();
            foreach (var handler in element.Handlers)
            {
                var label = $"Graph projection '{projection.Name}', edge '{edge.Type}'";
                if (!context.Events.TryGetValue(handler.Event, out var contract))
                {
                    Refuse($"{label}: the handled event is not declared.");
                    continue;
                }

                if (!seen.Add(handler.Event) && !handler.Removes) Refuse($"{label}: event '{contract.Name}' is handled more than once; Chronicle's model-bound graph API maps one handler per event.");
                CheckValue(label, "source", handler.Source, allowContext: false);
                CheckValue(label, "target", handler.Target, allowContext: false);
                if (edge.IdentifiedBy is not null) CheckValue(label, "identity", handler.Identity, allowContext: false);
                if (handler.Removes) continue;
                CheckMappings(label, edge.Properties, handler);
            }
        }

        void CheckMappings(string label, ImmutableArray<SemanticGraphProperty> properties, SemanticGraphHandler handler)
        {
            foreach (var mapping in handler.Mappings)
            {
                var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Name, mapping.Property, StringComparison.Ordinal));
                if (property is null)
                {
                    Refuse($"{label}: maps '{mapping.Property}', which the element does not declare.");
                    continue;
                }

                if (mapping.Factor is not null)
                {
                    Refuse($"{label}: '{mapping.Property}' is {mapping.Operation.ToString().ToLowerInvariant()}ed by a product; Chronicle's graph API adds or subtracts one event property.");
                    continue;
                }

                switch (mapping.Operation)
                {
                    case SemanticProjectionOperation.Set:
                        if (mapping.Source is SemanticProjectionLiteral literal && !SemanticGraphAdmission.IsAttributeConstant(property.Type, literal.Value))
                        {
                            Refuse($"{label}: '{mapping.Property}' is set to a literal that is not a text, boolean or whole-number constant of a property of that type, which a .NET attribute argument cannot carry.");
                        }
                        else
                        {
                            CheckValue(label, mapping.Property, mapping.Source, allowContext: true);
                        }

                        break;
                    case SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract:
                        if (mapping.Source is not SemanticProjectionEventProperty { Path.Length: 1 })
                        {
                            Refuse($"{label}: '{mapping.Property}' is {mapping.Operation.ToString().ToLowerInvariant()}ed from something other than one event property.");
                        }

                        break;
                    case SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement:
                        break;
                    default:
                        Refuse($"{label}: the '{mapping.Operation.ToString().ToLowerInvariant()}' of '{mapping.Property}' has no model-bound graph attribute.");
                        break;
                }
            }
        }

        void CheckValue(string label, string role, SemanticProjectionValue? value, bool allowContext)
        {
            var supported = value switch
            {
                SemanticProjectionEventSourceIdentity => true,
                SemanticProjectionEventProperty { Path.Length: 1 } => true,
                SemanticProjectionEventContextValue { Path: var path } => allowContext && path.Length != 0 && !path.Contains('.', StringComparison.Ordinal),
                SemanticProjectionLiteral { Value: SemanticTextValue or SemanticBooleanValue or SemanticNumberValue } => true,
                _ => false
            };
            if (!supported) Refuse($"{label}: the {role} reads a value Chronicle's model-bound graph API cannot express.");
        }

        // Elements are declared per graph, so two projections of one graph must not both own an element.
        void CheckSharedElements()
        {
            foreach (var group in context.Application.GraphProjections.GroupBy(projection => projection.Graph, StringComparer.Ordinal))
            {
                var owners = group.SelectMany(projection => projection.Nodes.Concat(projection.Edges).Select(element => (Projection: projection.Name, Element: element.Name)))
                    .GroupBy(owned => owned.Element, StringComparer.Ordinal).Where(owned => owned.Select(entry => entry.Projection).Distinct(StringComparer.Ordinal).Count() > 1);
                foreach (var owned in owners)
                {
                    Refuse($"Graph '{group.Key}' is built by several projections that all handle '{owned.Key}'; a model-bound graph type is the projection of its own element.");
                }
            }
        }

        void Refuse(string message) => Diagnostic(Unsupported, message);

        void Diagnostic(string code, string message) => diagnostics.Add(Error(code, context, message));
    }
}
