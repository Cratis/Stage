// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the named source definitions consumed by routed commands and filtered reducers.
/// </summary>
internal static class SemanticEventSourceArtifactRenderer
{
    internal static string ClassName(SemanticEventSource source) => GeneratedTypeNames.EventSourceName(source.Name);

    internal static IReadOnlyList<SemanticEventSource> Selected(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        SemanticSharedReferences.EventSources(context, slices);

    internal static string Namespace(SemanticApplicationContext context) => string.Join('.', new[] { context.RootNamespace }.Concat(context.Domain).Append("EventSources"));

    internal static bool IsIdentityName(string name) =>
        GeneratedPascalCase.From(name) == name && SyntaxFacts.IsValidIdentifier(name) &&
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None;

    internal static string StreamReference(SemanticEventSource source, SemanticEventStream stream, SemanticApplicationContext context) =>
        IsIdentityName(stream.Name)
            ? $"global::{Namespace(context)}.{ClassName(source)}.{stream.Name}"
            : CSharpCodeBuilder.StringLiteral(stream.StreamKind);

    internal static RenderedFile Render(SemanticEventSource source, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace(Namespace(context))
            .Summary($"Defines the {Identifiers.ToWords(source.Name)} event source.")
            .Attribute($"global::Cratis.Chronicle.EventSources.EventSourceAttribute({CSharpCodeBuilder.StringLiteral(source.SourceKind)})");
        foreach (var stream in source.Streams.OrderBy(stream => stream.StreamKind, StringComparer.Ordinal))
        {
            builder.Attribute($"global::Cratis.Chronicle.EventSources.EventStreamAttribute({StreamReference(source, stream, context)})");
        }
        RenderDeclaration(builder, source);
        return new(string.Join('/', context.Domain.Append("EventSources").Append($"{ClassName(source)}.cs")), builder.ToString())
        {
            Sources = [source.Id, .. source.Streams.Select(stream => stream.Id)]
        };
    }

    static void RenderDeclaration(CSharpCodeBuilder builder, SemanticEventSource source)
    {
        var declaration = $"public class {ClassName(source)} : global::Cratis.Chronicle.EventSources.IEventSource";
        var streams = source.Streams.Where(stream => IsIdentityName(stream.Name)).OrderBy(stream => stream.StreamKind, StringComparer.Ordinal).ToArray();
        if (streams.Length == 0)
        {
            builder.Line($"{declaration};");
            return;
        }
        builder.OpenBlock(declaration);
        foreach (var stream in streams)
        {
            if (stream != streams[0]) builder.BlankLine();
            builder.Summary($"Identifies the {Identifiers.ToWords(stream.Name)} stream.")
                .Line($"public const string {stream.Name} = {CSharpCodeBuilder.StringLiteral(stream.StreamKind)};");
        }
        builder.EndBlock();
    }
}
