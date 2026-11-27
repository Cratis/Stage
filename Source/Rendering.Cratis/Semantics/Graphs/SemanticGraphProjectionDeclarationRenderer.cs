// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Preserves a graph projection's declaration separately from its shared graph records.
/// </summary>
internal static class SemanticGraphProjectionDeclarationRenderer
{
    internal static IEnumerable<RenderedFile> Render(SemanticApplicationContext context)
    {
        foreach (var projection in context.Application.GraphProjections)
        {
            // Legacy models have no declaring slice. Keep their graph records without inventing ownership.
            if (projection.DeclaringSlice is not { } owner) continue;
            var located = context.Slice(owner);
            var graph = context.Application.Graphs.Single(graph => string.Equals(graph.Name, projection.Graph, StringComparison.Ordinal));
            var name = GeneratedTypeNames.GraphProjectionDeclarationName(projection.Name);
            var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
            builder.Pin(name);
            var graphNamespace = SemanticGraphNaming.Namespace(context, graph);
            var members = Members(graph, projection).Select(member => $"typeof(global::{graphNamespace}.{member})");
            var arguments = new[] { CSharpCodeBuilder.StringLiteral(projection.Name), CSharpCodeBuilder.StringLiteral(projection.Graph) }.Concat(members);
            builder.Summary($"Declares the {Identifiers.ToWords(projection.Name)} graph projection.")
                .Attribute($"global::Cratis.Arc.Slices.GraphProjectionDeclarationAttribute({string.Join(", ", arguments)})")
                .Line($"public sealed class {name};");
            yield return new(string.Join('/', SliceNaming.FolderPath(located.Path).Append($"{name}.cs")), builder.ToString()) { Sources = [owner] };
        }
    }

    static IEnumerable<string> Members(SemanticGraph graph, SemanticGraphProjection projection) =>
        projection.Nodes.Select(element => SemanticGraphNaming.NodeRecord(graph.Nodes.Single(node => string.Equals(node.Label, element.Name, StringComparison.Ordinal))))
            .Concat(projection.Edges.Select(element => SemanticGraphNaming.EdgeRecord(graph.Edges.Single(edge => string.Equals(edge.Type, element.Name, StringComparison.Ordinal)))));
}
