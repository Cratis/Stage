// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs;

/// <summary>
/// Names what a graph renders to: the namespace and folder of a graph, and the record of each node label and edge type.
/// </summary>
/// <remarks>
/// A node's label is the name of its record in Chronicle's model-bound graph API, so the label must already be a PascalCase identifier.
/// An edge's type is carried by <c language="csharp">[Edge]</c>, so its record is named from the type in PascalCase.
/// </remarks>
internal static class SemanticGraphNaming
{
    internal const string EndpointSource = "SourceKey";
    internal const string EndpointTarget = "TargetKey";

    internal static string Namespace(SemanticApplicationContext context, SemanticGraph graph) =>
        string.Join('.', new[] { context.RootNamespace }.Concat(context.Domain).Append("Graphs").Append(Identifiers.ToPascalCase(graph.Name)));

    internal static string Folder(SemanticApplicationContext context, SemanticGraph graph) =>
        string.Join('/', context.Domain.Append("Graphs").Append(Identifiers.ToPascalCase(graph.Name)));

    internal static string NodeRecord(SemanticGraphNode node) => node.Label;

    internal static string EdgeRecord(SemanticGraphEdge edge) => string.Concat(
        edge.Type.Split([.. edge.Type.Where(character => !char.IsLetterOrDigit(character)).Distinct()], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.All(character => !char.IsLetter(character) || char.IsUpper(character))
                ? string.Concat(char.ToUpperInvariant(word[0]).ToString(), word[1..].ToLowerInvariant())
                : string.Concat(char.ToUpperInvariant(word[0]).ToString(), word[1..])));

    internal static string Property(string name) => Identifiers.ToPascalCase(name);

    internal static bool IsIdentifier(string name) =>
        name.Length != 0 && SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None &&
        SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None;

    internal static bool NodeLabelIsRecordName(SemanticGraphNode node) =>
        IsIdentifier(node.Label) && string.Equals(Identifiers.ToPascalCase(node.Label), node.Label, StringComparison.Ordinal);

    internal static string ClassName(SemanticGraphSpecification specification) => $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_graphed";

    internal static string FileName(SemanticGraphSpecification specification) => $"{ClassName(specification)}.cs";
}
