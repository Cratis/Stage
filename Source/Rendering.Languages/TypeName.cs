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
    internal IEnumerable<string> Fields => new[] { Package, Name }.Concat(Nested);
}
