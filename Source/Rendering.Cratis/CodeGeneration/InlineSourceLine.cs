// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal sealed record InlineSourceLine(int Line, int LineCount, string Path)
{
    internal static Dictionary<string, InlineSourceLine>? Map(ArtifactRenderRequest request)
    {
        if (!SourceLineMappingInput.TryRead(request.Profile, out var mapping) || mapping is null) return null;

        var lines = new Dictionary<string, InlineSourceLine>(StringComparer.Ordinal);
        foreach (var requirement in request.ImplementationRequirements)
        {
            if (For(requirement, mapping) is { } line) lines[requirement.RequirementId] = line;
        }

        return lines;
    }

    static InlineSourceLine? For(SemanticImplementationRequirement requirement, SourceLineMappingInput.Mapping mapping)
    {
        var body = requirement.BodyLines;
        if (requirement.File is not null || body.IsDefaultOrEmpty) return null;
        if (Enumerable.Range(0, body.Length).Any(index => body[index].Line != body[0].Line + index)) return null;
        return mapping.PathOf(requirement.Source.Span.Document) is { } path ? new(body[0].Line, body.Length, path) : null;
    }
}
