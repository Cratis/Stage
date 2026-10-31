// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Rewrites the <c language="csharp">#line</c> paths a render produced from model-root-relative to relative to the folder of
/// the generated file that carries them, which is how Roslyn resolves a relative <c language="csharp">#line</c> path.
/// </summary>
static class SourceLineDirectives
{
    internal static IEnumerable<PlannedArtifact> Relocate(ArtifactRenderProfile profile, SourceLineMappingInput.Mapping mapping, IEnumerable<PlannedArtifact> artifacts)
    {
        var known = mapping.Documents.Keys.Select(mapping.PathOf).OfType<string>().ToHashSet(StringComparer.Ordinal);
        foreach (var artifact in artifacts) yield return Relocate(profile, known, artifact);
    }

    static PlannedArtifact Relocate(ArtifactRenderProfile profile, HashSet<string> known, PlannedArtifact artifact)
    {
        if (!artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) || artifact.Kind != PlannedArtifactKind.Text) return artifact;

        var encoding = new UTF8Encoding(false);
        var text = encoding.GetString([.. artifact.Bytes]);
        if (!text.Contains("#line ", StringComparison.Ordinal)) return artifact;

        var prefix = string.Concat(Enumerable.Repeat("../", ServiceLayout.Map(profile, artifact.RelativePath).Count(character => character == '/')));
        var edits = CSharpSyntaxTree.ParseText(text).GetRoot().DescendantTrivia(descendIntoTrivia: true)
            .Select(trivia => trivia.GetStructure()).OfType<LineDirectiveTriviaSyntax>()
            .Where(directive => directive.File.IsKind(SyntaxKind.StringLiteralToken) && known.Contains(directive.File.ValueText))
            .OrderByDescending(directive => directive.File.SpanStart).ToArray();
        if (edits.Length == 0) return artifact;

        foreach (var directive in edits)
        {
            text = text.Remove(directive.File.SpanStart, directive.File.Span.Length).Insert(directive.File.SpanStart, $"\"{prefix}{directive.File.ValueText}\"");
        }

        return PlannedArtifact.CreateVerbatimText(artifact.RelativePath, [.. encoding.GetBytes(text)], artifact.Sources);
    }
}
