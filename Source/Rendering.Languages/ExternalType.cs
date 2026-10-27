// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Supplies an external type's direct member types and structured superclass/interface edges.
/// </summary>
/// <param name="Type">The external type.</param>
/// <param name="DeclaredMemberTypes">Direct member type simple names. Omitted/default metadata is unknown; only explicit <see cref="KnownTypes{T}.None"/> proves there are no direct member types.</param>
public sealed record ExternalType(TypeName Type, KnownTypes<string> DeclaredMemberTypes = default)
{
    /// <summary>
    /// Gets superclass and interface metadata keyed by erased structured names, not generic argument lists.
    /// Omitted/default metadata is unknown, independently of the member set. Only explicit
    /// <see cref="KnownTypes{T}.None"/> proves there are no supertype edges.
    /// </summary>
    public KnownTypes<TypeName> Supertypes { get; init; }

    internal IEnumerable<string> Fields => Type.Fields.Concat(DeclaredMemberTypes.IsKnown ? DeclaredMemberTypes.Values : [])
        .Concat(Supertypes.IsKnown ? Supertypes.Values.SelectMany(type => type.Fields) : []);
}
