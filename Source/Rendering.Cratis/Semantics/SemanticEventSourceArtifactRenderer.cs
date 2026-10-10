// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the named source definitions consumed by routed commands.
/// </summary>
internal static class SemanticEventSourceArtifactRenderer
{
    internal static string ClassName(SemanticEventSource source) => GeneratedTypeNames.EventSourceName(source.Name);

    internal static IReadOnlyList<SemanticEventSource> Selected(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        if (context.Request.Scope.Kind == ArtifactRenderScopeKind.Application) return context.Application.EventSources;
        var references = slices.SelectMany(slice => slice.Slice.Commands).Select(command => command.Route?.Source).ToHashSet();
        return [.. context.Application.EventSources.Where(source => references.Contains(source.Id))];
    }

    internal static string Namespace(SemanticApplicationContext context) => string.Join('.', new[] { context.RootNamespace }.Concat(context.Domain).Append("EventSources"));

    internal static RenderedFile Render(SemanticEventSource source, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace(Namespace(context))
            .Summary($"Defines the {Identifiers.ToWords(source.Name)} event source.")
            .Attribute($"global::Cratis.Chronicle.EventSources.EventSourceAttribute({CSharpCodeBuilder.StringLiteral(source.SourceKind)})");
        foreach (var stream in source.Streams.OrderBy(stream => stream.StreamKind, StringComparer.Ordinal))
        {
            builder.Attribute($"global::Cratis.Chronicle.EventSources.EventStreamAttribute({CSharpCodeBuilder.StringLiteral(stream.StreamKind)})");
        }
        builder.Line($"public class {ClassName(source)} : global::Cratis.Chronicle.EventSources.IEventSource;");
        return new(string.Join('/', context.Domain.Append("EventSources").Append($"{ClassName(source)}.cs")), builder.ToString())
        {
            Sources = [source.Id, .. source.Streams.Select(stream => stream.Id)]
        };
    }
}
