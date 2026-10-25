// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Prefixes every generated code file with the caller's header, written as comments in the file's language.
/// </summary>
/// <remarks>
/// This is the only place a header is written: renderers and the scaffold emit none, so an application that asks for
/// no header gets files that start with their code. Configuration, markup and documentation files never get one.
/// </remarks>
static class GeneratedFileHeader
{
    static readonly string[] _lineCommentExtensions = [".cs", ".ts", ".tsx", ".js", ".mjs", ".cjs"];

    /// <summary>
    /// Applies the header carried by a profile to the planned artifacts.
    /// </summary>
    /// <param name="profile">The admitted render profile.</param>
    /// <param name="artifacts">The planned artifacts.</param>
    /// <returns>The artifacts, with code files prefixed when the profile carries a header.</returns>
    public static ImmutableArray<PlannedArtifact> Apply(ArtifactRenderProfile profile, IEnumerable<PlannedArtifact> artifacts)
    {
        if (!FileHeaderInput.TryRead(profile, out var lines) || lines.IsEmpty) return [.. artifacts];
        var header = Comment(lines);

        return [.. artifacts.Select(artifact => Apply(header, artifact))];
    }

    /// <summary>
    /// Writes the lines as line comments followed by one blank line.
    /// </summary>
    /// <param name="lines">The plain-text header lines.</param>
    /// <returns>The comment block.</returns>
    public static string Comment(ImmutableArray<string> lines)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            var text = line.TrimEnd();
            builder.Append(text.Length == 0 ? "//" : $"// {text}").Append('\n');
        }

        return builder.Append('\n').ToString();
    }

    static PlannedArtifact Apply(string header, PlannedArtifact artifact)
    {
        if (artifact.Kind != PlannedArtifactKind.Text || !_lineCommentExtensions.Any(extension => artifact.RelativePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return artifact;
        }

        var content = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        return artifact.Sources.IsEmpty
            ? PlannedArtifact.CreateText(artifact.RelativePath, header + content)
            : PlannedArtifact.CreateText(artifact.RelativePath, header + content, artifact.Sources);
    }
}
