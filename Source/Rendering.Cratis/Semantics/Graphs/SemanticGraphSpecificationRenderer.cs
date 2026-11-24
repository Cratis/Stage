// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Renders a graph specification as a Debug specification over Chronicle's <c language="csharp">GraphScenario</c>: the events the
/// specification states are appended, and every <c language="text">then graph</c> element is read back from the graph the projections built.
/// </summary>
internal static class SemanticGraphSpecificationRenderer
{
    const string Scenario = "global::Cratis.Chronicle.Testing.Graphs.GraphScenario";
    const string Fact = "[global::Xunit.FactAttribute]";

    /// <summary>
    /// Renders one graph specification.
    /// </summary>
    /// <param name="specification">The graph specification, already admitted.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    internal static RenderedFile Render(SemanticGraphSpecification specification, SemanticApplicationContext context)
    {
        var graphs = context.Application.Graphs.ToDictionary(graph => graph.Name, StringComparer.Ordinal);
        var behavior = SemanticGraphNaming.ClassName(specification);
        var builder = context.SpecificationBuilder().Namespace($"{string.Join('.', new[] { context.RootNamespace }.Concat(context.Domain).Append("Graphs").Append("Specifications"))}.{behavior}")
            .Using("Cratis.Specifications");
        var types = new SemanticTypeSystem(context, builder);
        var elements = specification.ThenGraphs.SelectMany(step => step.Elements.Select(element => (Graph: graphs[step.Graph], Element: element))).ToArray();

        builder.Summary($"Specifies {Identifiers.ToWords(specification.Name)}.")
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly {Scenario} _scenario = new();");
        for (var index = 0; index < elements.Length; index++)
        {
            builder.Line($"{ElementType(elements[index].Graph, elements[index].Element, context)}? _element{index};");
        }

        RenderSeeds(builder, specification, graphs, types);
        builder.BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        foreach (var fact in specification.Given.Concat(specification.WhenAppended is null ? [] : [specification.WhenAppended]))
        {
            builder.Line($"await _scenario.Append({SemanticSpecificationEventSources.Expression(context, types, fact)}, {NewEvent(fact, context, types)});");
        }

        for (var index = 0; index < elements.Length; index++)
        {
            builder.Line($"_element{index} = await {Read(elements[index].Graph, elements[index].Element, context, types)};");
        }

        builder.EndBlock().BlankLine();
        for (var index = 0; index < elements.Length; index++)
        {
            Expect(builder, elements[index].Graph, elements[index].Element, index, types, context);
        }

        builder.Line("/// <inheritdoc/>").Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        var path = Path.Combine([.. context.Domain, "Graphs", "Specifications", SemanticGraphNaming.FileName(specification)]);
        return new(path, types.SpecificationContent(builder)) { Sources = [context.Application.Id] };
    }

    static void RenderSeeds(CSharpCodeBuilder builder, SemanticGraphSpecification specification, Dictionary<string, SemanticGraph> graphs, SemanticTypeSystem types)
    {
        if (specification.GivenGraphs.IsDefaultOrEmpty) return;
        builder.BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Establish()");
        foreach (var step in specification.GivenGraphs)
        {
            var graph = graphs[step.Graph];
            foreach (var element in step.Elements)
            {
                builder.Line($"await {Seed(graph, element, types)};");
            }
        }

        builder.EndBlock();
    }

    static string Seed(SemanticGraph graph, SemanticGraphElementStep element, SemanticTypeSystem types)
    {
        if (element.IsNode)
        {
            var node = Node(graph, element.Name);
            return $"_scenario.SeedNode({CSharpCodeBuilder.StringLiteral(node.Label)}, {KeyValue(element.Key, SemanticGraphArtifactRenderer.KeyOf(node).Type, types)}, {SeedProperties(element, node.Properties, types)})";
        }

        var edge = Edge(graph, element.Name);
        var source = KeyValue(element.Key, SemanticGraphArtifactRenderer.KeyOf(Node(graph, edge.Source)).Type, types);
        var target = KeyValue(element.Target!, SemanticGraphArtifactRenderer.KeyOf(Node(graph, edge.Target)).Type, types);
        var identity = element.Identity is null ? "null" : KeyValue(element.Identity, edge.Properties.Single(property => string.Equals(property.Name, edge.IdentifiedBy, StringComparison.Ordinal)).Type, types);
        return $"_scenario.SeedEdge({CSharpCodeBuilder.StringLiteral(edge.Type)}, {source}, {target}, {identity}, {SeedProperties(element, edge.Properties, types)})";
    }

    static string SeedProperties(SemanticGraphElementStep element, ImmutableArray<SemanticGraphProperty> properties, SemanticTypeSystem types)
    {
        var entries = element.Values.Select(value =>
        {
            var property = properties.Single(candidate => string.Equals(candidate.Name, value.Property, StringComparison.Ordinal));
            var member = SemanticGraphNaming.Property(property.Name);
            var name = member.Length > 1 && char.IsUpper(member[1]) ? member : Identifiers.ToCamelCase(member);
            return $"[{CSharpCodeBuilder.StringLiteral(name)}] = {SeedValue(value.Value, property.Type, types)}";
        });
        return $"new global::System.Collections.Generic.Dictionary<string, object?> {{ {string.Join(", ", entries)} }}";
    }

    static string SeedValue(SemanticValue value, SemanticTypeReference type, SemanticTypeSystem types)
    {
        if (type.IsCollection && value is SemanticArrayValue array)
        {
            var elementType = type with { IsCollection = false, IsOptional = false };
            return $"new object?[] {{ {string.Join(", ", array.Values.Select(element => SeedValue(element, elementType, types)))} }}";
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = types.ConceptPrimitive(type) }
            : type;
        return types.Value(value, primitive);
    }

    static string NewEvent(SemanticSpecificationEvent fact, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var contract = context.Events[fact.EventContract];
        var arguments = contract.Properties.Select(property => types.Value(fact.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        return $"new {types.EventType(contract)}({string.Join(", ", arguments)})";
    }

    static string ElementType(SemanticGraph graph, SemanticGraphElementStep element, SemanticApplicationContext context) =>
        $"global::{SemanticGraphNaming.Namespace(context, graph)}.{RecordOf(graph, element)}";

    static string RecordOf(SemanticGraph graph, SemanticGraphElementStep element) => element.IsNode
        ? SemanticGraphNaming.NodeRecord(Node(graph, element.Name))
        : SemanticGraphNaming.EdgeRecord(Edge(graph, element.Name));

    static SemanticGraphNode Node(SemanticGraph graph, string label) => graph.Nodes.Single(node => string.Equals(node.Label, label, StringComparison.Ordinal));

    static SemanticGraphEdge Edge(SemanticGraph graph, string type) => graph.Edges.Single(edge => string.Equals(edge.Type, type, StringComparison.Ordinal));

    static string Read(SemanticGraph graph, SemanticGraphElementStep element, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var record = ElementType(graph, element, context);
        if (element.IsNode)
        {
            return $"_scenario.Node<{record}>({KeyValue(element.Key, SemanticGraphArtifactRenderer.KeyOf(Node(graph, element.Name)).Type, types)})";
        }

        var edge = Edge(graph, element.Name);
        var arguments = new List<string>
        {
            KeyValue(element.Key, SemanticGraphArtifactRenderer.KeyOf(Node(graph, edge.Source)).Type, types),
            KeyValue(element.Target!, SemanticGraphArtifactRenderer.KeyOf(Node(graph, edge.Target)).Type, types)
        };
        if (element.Identity is not null) arguments.Add(KeyValue(element.Identity, edge.Properties.Single(property => string.Equals(property.Name, edge.IdentifiedBy, StringComparison.Ordinal)).Type, types));
        return $"_scenario.Edge<{record}>({string.Join(", ", arguments)})";
    }

    // The graph stores the key as the value it holds, so a concept is read through its value.
    static string KeyValue(SemanticValue value, SemanticTypeReference type, SemanticTypeSystem types) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? $"{types.Value(value, type)}.Value" : types.Value(value, type);

    static void Expect(CSharpCodeBuilder builder, SemanticGraph graph, SemanticGraphElementStep element, int index, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var name = Identifiers.ToSnakeCase(element.Name);
        var variable = $"_element{index}";
        if (element.Absent)
        {
            builder.Line($"{Fact} void should_not_have_{name}_{index + 1}() => {variable}.ShouldBeNull();");
            return;
        }

        if (element.Values.IsEmpty) builder.Line($"{Fact} void should_have_{name}_{index + 1}() => {variable}.ShouldNotBeNull();");
        var properties = element.IsNode ? Node(graph, element.Name).Properties : Edge(graph, element.Name).Properties;
        foreach (var value in element.Values)
        {
            var property = properties.Single(candidate => string.Equals(candidate.Name, value.Property, StringComparison.Ordinal));
            var member = $"{variable}!.{SemanticGraphNaming.Property(property.Name)}";
            var fact = $"should_have_{name}_{index + 1}_{Identifiers.ToSnakeCase(property.Name)}";
            if (property.Type.IsCollection && value.Value is SemanticArrayValue array)
            {
                var elementType = property.Type with { IsCollection = false, IsOptional = false };
                var expected = string.Join(", ", array.Values.Select(candidate => types.Value(candidate, elementType)));
                builder.Line($"{Fact} void {fact}() => global::Xunit.Assert.True(global::System.Linq.Enumerable.SequenceEqual({member}, new {types.Type(elementType)}[] {{ {expected} }}));");
            }
            else
            {
                builder.Line($"{Fact} void {fact}() => {member}.ShouldEqual({types.Value(value.Value, property.Type)});");
            }
        }

        _ = context;
    }
}
