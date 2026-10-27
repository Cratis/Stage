// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Identifies a model type explicitly, without inferring package or nesting from capitalization.
/// </summary>
/// <param name="Package">The package, or an empty string for the unnamed package.</param>
/// <param name="Name">The top-level simple name.</param>
/// <param name="Nested">The ordered nested-type path below the top-level name.</param>
public sealed record TypeName(string Package, string Name, IReadOnlyList<string> Nested)
{
    /// <summary>
    /// Gets the fully qualified source name, retaining the explicit package boundary.
    /// </summary>
    public string QualifiedName => string.Join('.', new[] { Package, Name }.Where(part => part.Length > 0).Concat(Nested));

    /// <summary>
    /// Gets the innermost simple name.
    /// </summary>
    public string SimpleName => Nested.Count == 0 ? Name : Nested[^1];

    /// <summary>
    /// Gets the enclosing type path without the package, or an empty string for a top-level type.
    /// </summary>
    public string EnclosingScope => Nested.Count == 0 ? string.Empty : string.Join('.', new[] { Name }.Concat(Nested.SkipLast(1)));

    /// <summary>
    /// Gets the top-level type without any nested path.
    /// </summary>
    public TypeName TopLevel => new(Package, Name, []);

    internal IEnumerable<string> Fields => new[] { Package, Name }.Concat(Nested);
}
