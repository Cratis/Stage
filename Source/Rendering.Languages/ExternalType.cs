// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Supplies an external type's direct member types and structured superclass/interface edges.
/// </summary>
/// <param name="Type">The external type.</param>
/// <param name="DeclaredMemberTypes">Direct member type simple names. Null means unknown; an empty list proves there are no direct member types.</param>
public sealed record ExternalType(TypeName Type, IReadOnlyList<string>? DeclaredMemberTypes = null)
{
    /// <summary>
    /// Gets the superclass and interfaces. An empty list declares no supertype edges.
    /// </summary>
    public IReadOnlyList<TypeName> Supertypes { get; init; } = [];

    internal IEnumerable<string> Fields => Type.Fields.Concat(DeclaredMemberTypes ?? []).Concat(Supertypes.SelectMany(type => type.Fields));
}
