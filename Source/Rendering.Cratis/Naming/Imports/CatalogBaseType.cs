// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a class a generated type may derive from, with the names it declares that a derived type sees by a simple
/// name: a member named like a type hides that type in an expression, and a nested type hides it everywhere.
/// </summary>
/// <param name="Projects">The projects that see the class.</param>
/// <param name="FullName">The full metadata name, such as <c language="csharp">Cratis.Concepts.ConceptAs`1</c>.</param>
/// <param name="BaseFullName">The full metadata name of its base class, empty for <see cref="object"/>.</param>
/// <param name="Members">The public and protected member names it declares, sorted ordinally.</param>
/// <param name="NestedTypes">The public and protected nested type names it declares, sorted ordinally.</param>
internal sealed record CatalogBaseType(
    GeneratedProjects Projects,
    string FullName,
    string BaseFullName,
    ImmutableArray<string> Members,
    ImmutableArray<string> NestedTypes);
