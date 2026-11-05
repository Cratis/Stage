// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Renders each graph as the model-bound records of Chronicle's graph projection API: a <c language="csharp">[Node]</c> record per node label
/// and an <c language="csharp">[Edge]</c> record per edge type, carrying the handlers of the graph projections that build them.
/// </summary>
/// <remarks>
/// The attributes are Chronicle's own, so what the projection does is what Chronicle's graph engine does with them. The fluent
/// <c language="csharp">IGraphProjection</c> is not needed: every construct Screenplay can say is either expressible with attributes or refused
/// by <see cref="SemanticGraphAdmission"/> before this renderer runs.
/// </remarks>
internal static class SemanticGraphArtifactRenderer
{
    const string Graphs = "global::Cratis.Chronicle.Projections.Graphs";
    const string ModelBound = "global::Cratis.Chronicle.Projections.ModelBound";
    const string Key = "global::Cratis.Chronicle.Keys.KeyAttribute";
    const string NoAutoMap = "global::Cratis.Chronicle.Projections.NoAutoMapAttribute";

    /// <summary>
    /// Renders every node and edge record of the application's graphs.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The rendered files in graph, node, then edge order.</returns>
    internal static IEnumerable<RenderedFile> Render(SemanticApplicationContext context)
    {
        var application = context.Application;
        foreach (var graph in application.Graphs)
        {
            var projections = application.GraphProjections.Where(projection => string.Equals(projection.Graph, graph.Name, StringComparison.Ordinal)).ToArray();
            foreach (var node in graph.Nodes)
            {
                var handlers = projections.SelectMany(projection => projection.Nodes).Where(element => string.Equals(element.Name, node.Label, StringComparison.Ordinal)).SelectMany(element => element.Handlers).ToArray();
                yield return RenderNode(context, graph, node, handlers);
            }

            foreach (var edge in graph.Edges)
            {
                var handlers = projections.SelectMany(projection => projection.Edges).Where(element => string.Equals(element.Name, edge.Type, StringComparison.Ordinal)).SelectMany(element => element.Handlers).ToArray();
                yield return RenderEdge(context, graph, edge, handlers);
            }
        }
    }

    internal static SemanticGraphProperty KeyOf(SemanticGraphNode node) => node.Properties.Single(property => property.IsKey);

    static RenderedFile RenderNode(SemanticApplicationContext context, SemanticGraph graph, SemanticGraphNode node, SemanticGraphHandler[] handlers)
    {
        var builder = context.CodeBuilder().Namespace(SemanticGraphNaming.Namespace(context, graph));
        var types = new SemanticTypeSystem(context, builder);
        var name = SemanticGraphNaming.NodeRecord(node);
        builder.Pin(name);
        builder.Summary($"Represents a {Identifiers.ToWords(node.Label)} node of the {Identifiers.ToWords(graph.Name)} graph.");
        foreach (var property in node.Properties) builder.Parameter(SemanticGraphNaming.Property(property.Name), $"The {Identifiers.ToWords(property.Name)}.");
        builder.Attribute($"{Graphs}.GraphAttribute({CSharpCodeBuilder.StringLiteral(graph.Name)})")
            .Attribute($"{Graphs}.NodeAttribute")
            .Attribute(NoAutoMap);
        foreach (var handler in handlers)
        {
            var @event = types.EventType(context.Events[handler.Event]);
            builder.Attribute(handler.Removes ? RemovedWith(handler, @event, context) : FromEvent(handler, @event, context));
        }

        var parameters = node.Properties.Select(property => Parameter(property, handlers, types, context, isIdentity: false)).ToArray();
        builder.Line($"public record {name}({string.Join(", ", parameters)});");
        return new(Path.Combine(SemanticGraphNaming.Folder(context, graph), $"{name}.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static RenderedFile RenderEdge(SemanticApplicationContext context, SemanticGraph graph, SemanticGraphEdge edge, SemanticGraphHandler[] handlers)
    {
        var builder = context.CodeBuilder().Namespace(SemanticGraphNaming.Namespace(context, graph));
        var types = new SemanticTypeSystem(context, builder);
        var name = SemanticGraphNaming.EdgeRecord(edge);
        var source = graph.Nodes.Single(node => string.Equals(node.Label, edge.Source, StringComparison.Ordinal));
        var target = graph.Nodes.Single(node => string.Equals(node.Label, edge.Target, StringComparison.Ordinal));
        builder.Pin(name);
        builder.Summary($"Represents a {Identifiers.ToWords(edge.Type)} edge from {Identifiers.ToWords(edge.Source)} to {Identifiers.ToWords(edge.Target)} in the {Identifiers.ToWords(graph.Name)} graph.");
        builder.Parameter(SemanticGraphNaming.EndpointSource, $"The key of the {Identifiers.ToWords(edge.Source)} the edge leaves.")
            .Parameter(SemanticGraphNaming.EndpointTarget, $"The key of the {Identifiers.ToWords(edge.Target)} the edge enters.");
        foreach (var property in edge.Properties) builder.Parameter(SemanticGraphNaming.Property(property.Name), $"The {Identifiers.ToWords(property.Name)}.");
        builder.Attribute($"{Graphs}.GraphAttribute({CSharpCodeBuilder.StringLiteral(graph.Name)})")
            .Attribute($"{Graphs}.EdgeAttribute({CSharpCodeBuilder.StringLiteral(edge.Type)})")
            .Attribute(NoAutoMap);
        foreach (var handler in handlers)
        {
            var @event = types.EventType(context.Events[handler.Event]);
            builder.Attribute(EdgeHandler(handler.Removes ? "EdgeRemovedWithAttribute" : "EdgeFromAttribute", handler, @event, edge, context));
        }

        var parameters = new List<string>
        {
            $"[{Graphs}.SourceAttribute<{SemanticGraphNaming.NodeRecord(source)}>] {types.Type(KeyOf(source).Type)} {SemanticGraphNaming.EndpointSource}",
            $"[{Graphs}.TargetAttribute<{SemanticGraphNaming.NodeRecord(target)}>] {types.Type(KeyOf(target).Type)} {SemanticGraphNaming.EndpointTarget}"
        };
        parameters.AddRange(edge.Properties.Select(property => Parameter(property, handlers, types, context, string.Equals(property.Name, edge.IdentifiedBy, StringComparison.Ordinal))));
        builder.Line($"public record {name}({string.Join(", ", parameters)});");
        return new(Path.Combine(SemanticGraphNaming.Folder(context, graph), $"{name}.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static string Parameter(SemanticGraphProperty property, SemanticGraphHandler[] handlers, SemanticTypeSystem types, SemanticApplicationContext context, bool isIdentity)
    {
        var attributes = new List<string>();
        if (property.IsKey || isIdentity) attributes.Add(Key);
        foreach (var handler in handlers.Where(handler => !handler.Removes))
        {
            var @event = context.Events[handler.Event];
            var eventType = types.EventType(@event);

            // The key of a node and the identity of an edge are already said by the handler; mapping them again would only repeat it.
            var said = Said(property, isIdentity, handler);
            attributes.AddRange(handler.Mappings.Where(mapping => string.Equals(mapping.Property, property.Name, StringComparison.Ordinal) && !(mapping.Operation == SemanticProjectionOperation.Set && Same(mapping.Source, said)))
                .Select(mapping => Mapping(mapping, property, eventType, @event, types)));
        }

        var prefix = string.Concat(attributes.Select(attribute => $"[{attribute}] "));
        return $"{prefix}{types.Type(property.Type)} {SemanticGraphNaming.Property(property.Name)}";
    }

    static string Mapping(SemanticGraphMapping mapping, SemanticGraphProperty property, string eventType, SemanticEventContract @event, SemanticTypeSystem types) => mapping.Operation switch
    {
        SemanticProjectionOperation.Add => $"{ModelBound}.AddFromAttribute<{eventType}>(nameof({eventType}.{EventProperty(mapping.Source, @event)}))",
        SemanticProjectionOperation.Subtract => $"{ModelBound}.SubtractFromAttribute<{eventType}>(nameof({eventType}.{EventProperty(mapping.Source, @event)}))",
        SemanticProjectionOperation.Increment => $"{ModelBound}.IncrementAttribute<{eventType}>",
        SemanticProjectionOperation.Decrement => $"{ModelBound}.DecrementAttribute<{eventType}>",
        _ => mapping.Source switch
        {
            SemanticProjectionEventProperty => $"{ModelBound}.SetFromAttribute<{eventType}>(nameof({eventType}.{EventProperty(mapping.Source, @event)}))",
            SemanticProjectionEventSourceIdentity => $"{ModelBound}.SetFromContextAttribute<{eventType}>({CSharpCodeBuilder.StringLiteral("EventSourceId")})",
            SemanticProjectionEventContextValue context => $"{ModelBound}.SetFromContextAttribute<{eventType}>({CSharpCodeBuilder.StringLiteral(Identifiers.ToPascalCase(context.Path))})",
            SemanticProjectionLiteral literal => $"{ModelBound}.SetValueAttribute<{eventType}>({types.Value(literal.Value, property.Type)})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionValue), Describe(mapping.Source))
        }
    };

    static string FromEvent(SemanticGraphHandler handler, string eventType, SemanticApplicationContext context) => handler.Key switch
    {
        SemanticProjectionEventProperty property => $"{ModelBound}.FromEventAttribute<{eventType}>(nameof({eventType}.{EventProperty(property, context.Events[handler.Event])}))",
        SemanticProjectionLiteral literal => $"{ModelBound}.FromEventAttribute<{eventType}>(ConstantKey = {CSharpCodeBuilder.StringLiteral(Constant(literal.Value))})",
        _ => $"{ModelBound}.FromEventAttribute<{eventType}>"
    };

    static string RemovedWith(SemanticGraphHandler handler, string eventType, SemanticApplicationContext context)
    {
        var arguments = new List<string>();
        if (handler.Key is SemanticProjectionEventProperty property) arguments.Add($"nameof({eventType}.{EventProperty(property, context.Events[handler.Event])})");
        if (handler.Detach) arguments.Add("Detach = true");
        return arguments.Count == 0 ? $"{ModelBound}.RemovedWithAttribute<{eventType}>" : $"{ModelBound}.RemovedWithAttribute<{eventType}>({string.Join(", ", arguments)})";
    }

    static string EdgeHandler(string attribute, SemanticGraphHandler handler, string eventType, SemanticGraphEdge edge, SemanticApplicationContext context)
    {
        var @event = context.Events[handler.Event];
        var arguments = new List<string>
        {
            $"sourceKey: {EdgeKey(handler.Source, eventType, @event)}",
            $"targetKey: {EdgeKey(handler.Target, eventType, @event)}"
        };
        if (edge.IdentifiedBy is not null) arguments.Add($"identity: {EdgeKey(handler.Identity, eventType, @event)}");
        return $"{Graphs}.{attribute}<{eventType}>({string.Join(", ", arguments)})";
    }

    static string EdgeKey(SemanticProjectionValue? value, string eventType, SemanticEventContract @event) => value switch
    {
        SemanticProjectionEventProperty property => $"nameof({eventType}.{EventProperty(property, @event)})",
        SemanticProjectionLiteral literal => CSharpCodeBuilder.StringLiteral($"$value({Constant(literal.Value)})"),
        SemanticProjectionEventSourceIdentity => "global::Cratis.Chronicle.WellKnownExpressions.EventSourceId",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionValue), Describe(value))
    };

    static string EventProperty(SemanticProjectionValue? value, SemanticEventContract @event) =>
        value is SemanticProjectionEventProperty property
            ? Identifiers.ToPascalCase(@event.Properties.Single(candidate => candidate.Id == property.Path[0]).Name)
            : throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionValue), Describe(value));

    static SemanticProjectionValue? Said(SemanticGraphProperty property, bool isIdentity, SemanticGraphHandler handler)
    {
        if (property.IsKey) return handler.Key;
        return isIdentity ? handler.Identity : null;
    }

    static bool Same(SemanticProjectionValue? left, SemanticProjectionValue? right) => (left, right) switch
    {
        (SemanticProjectionEventSourceIdentity, SemanticProjectionEventSourceIdentity) => true,
        (SemanticProjectionEventProperty first, SemanticProjectionEventProperty second) => first.Path.SequenceEqual(second.Path),
        _ => false
    };

    static string Describe(SemanticProjectionValue? value) => value is null ? "none" : value.GetType().Name;

    static string Constant(SemanticValue value) => value switch
    {
        SemanticTextValue text => text.Value,
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValue), value.Kind.ToString())
    };
}
