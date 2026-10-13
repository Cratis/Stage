// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders observation metadata independently of append destinations.
/// </summary>
internal static class SemanticObserverFilterRendering
{
    internal static void Attribute(CSharpCodeBuilder builder, SemanticObserverFilter filter, SemanticApplicationContext context)
    {
        var source = context.Application.EventSources.Single(source => source.Id == filter.Source);
        if (filter.Stream is { } stream)
        {
            var sourceType = $"global::{SemanticEventSourceArtifactRenderer.Namespace(context)}.{SemanticEventSourceArtifactRenderer.ClassName(source)}";
            var streamKind = source.Streams.Single(candidate => candidate.Id == stream).StreamKind;
            builder.Attribute($"global::Cratis.Chronicle.EventSources.FromEventSourceAttribute<{sourceType}>({CSharpCodeBuilder.StringLiteral(streamKind)})");
        }
        else
        {
            builder.Attribute($"global::Cratis.Chronicle.Events.EventSourceTypeAttribute({CSharpCodeBuilder.StringLiteral(source.SourceKind)})");
        }
    }
}
