// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a type visible to a generated project, as the catalog records it.
/// </summary>
/// <param name="Projects">The projects that see the type.</param>
/// <param name="Namespace">The namespace, empty for the global namespace.</param>
/// <param name="MetadataName">The metadata name inside the namespace: generic arity after a backtick and nested types after a plus, such as <c language="csharp">List`1+Enumerator</c>.</param>
/// <param name="Kind">The kind of type: class, attribute, static, struct, interface, enum or delegate.</param>
internal sealed record CatalogedType(GeneratedProjects Projects, string Namespace, string MetadataName, string Kind)
{
    /// <summary>
    /// Gets a value indicating whether the type is declared directly in its namespace rather than nested in another type.
    /// </summary>
    public bool IsTopLevel => !MetadataName.Contains('+', StringComparison.Ordinal);

    /// <summary>
    /// Gets the simple name of the innermost type, without its generic arity.
    /// </summary>
    public string Name => CatalogNames.SimpleName(MetadataName[(MetadataName.LastIndexOf('+') + 1)..]);

    /// <summary>
    /// Gets the generic arity of the innermost type.
    /// </summary>
    public int Arity => CatalogNames.Arity(MetadataName[(MetadataName.LastIndexOf('+') + 1)..]);
}
