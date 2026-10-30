// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Describes a language's line comments and sequences unsafe within them.
/// </summary>
/// <param name="LinePrefix">The line comment introducer.</param>
/// <param name="RefusedSequences">Ordinal sequences that cannot safely appear in a comment.</param>
public sealed record ArtifactCommentSyntax(string LinePrefix, ImmutableArray<string> RefusedSequences);

/// <summary>
/// Maps file extensions to comment syntax without writing target content.
/// </summary>
public sealed class ArtifactCommentSyntaxes
{
    readonly ImmutableDictionary<string, ArtifactCommentSyntax> _byExtension;

    ArtifactCommentSyntaxes(ImmutableDictionary<string, ArtifactCommentSyntax> byExtension) => _byExtension = byExtension;

    /// <summary>
    /// Gets the standard code extensions; content and configuration extensions are absent.
    /// </summary>
    public static ArtifactCommentSyntaxes Default { get; } = Create(
        new[] { ".cs", ".ts", ".tsx", ".mts", ".cts", ".js", ".jsx", ".mjs", ".cjs", ".kt", ".kts", ".go" }
            .Select(extension => KeyValuePair.Create(extension, new ArtifactCommentSyntax("//", [])))
            .Append(KeyValuePair.Create(".java", new ArtifactCommentSyntax("//", ["\\u"]))));

    /// <summary>
    /// Creates an immutable, case-insensitive extension table.
    /// </summary>
    /// <param name="byExtension">The extension and syntax pairs.</param>
    /// <returns>The syntax table.</returns>
    public static ArtifactCommentSyntaxes Create(IEnumerable<KeyValuePair<string, ArtifactCommentSyntax>> byExtension) =>
        new(byExtension.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Adds or replaces one extension without changing this table.
    /// </summary>
    /// <param name="extension">The extension, including its leading dot.</param>
    /// <param name="syntax">The comment syntax.</param>
    /// <returns>A new syntax table.</returns>
    public ArtifactCommentSyntaxes With(string extension, ArtifactCommentSyntax syntax) => new(_byExtension.SetItem(extension, syntax));

    /// <summary>
    /// Finds the syntax for the final path segment's extension.
    /// </summary>
    /// <param name="relativePath">The artifact path.</param>
    /// <param name="syntax">The syntax when found.</param>
    /// <returns>Whether the extension has a registered syntax.</returns>
    public bool TryGet(string relativePath, out ArtifactCommentSyntax syntax)
    {
        var dot = relativePath.LastIndexOf('.');
        var slash = Math.Max(relativePath.LastIndexOf('/'), relativePath.LastIndexOf('\\'));
        syntax = null!;
        return dot > slash && _byExtension.TryGetValue(relativePath[dot..], out syntax!);
    }
}
