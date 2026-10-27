// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Describes a declaration after allocation. Source scope spellings remain lookup keys, never parent identities.
/// </summary>
/// <param name="Package">The explicitly supplied package.</param>
/// <param name="Scope">The preferred enclosing type path.</param>
/// <param name="Preferred">The requested simple name.</param>
/// <param name="Name">The allocated simple name.</param>
/// <param name="Owner">The stable declaration owner.</param>
/// <param name="File">The stable file identity.</param>
/// <param name="External">Whether the declaration is supplied externally and cannot be renamed.</param>
/// <param name="ParentOwner">The proven enclosing declaration identity.</param>
/// <param name="AllocatedScope">The allocated enclosing type path.</param>
public sealed record DeclaredType(string Package, string Scope, string Preferred, string Name, string Owner, string File, bool External, string ParentOwner, string AllocatedScope)
{
    /// <summary>
    /// Gets preferred, erased superclass and interface names. Omitted/default metadata remains unknown.
    /// </summary>
    public KnownTypes<TypeName> Supertypes { get; init; }

    /// <summary>
    /// Gets an external type's direct member metadata, unknown by default. Generated members are obtained from declaration identities and literal source.
    /// </summary>
    public KnownTypes<string> DeclaredMemberTypes { get; init; }

    /// <summary>
    /// Gets the final structured type name, including allocated parents.
    /// </summary>
    public TypeName Final => AllocatedScope.Length == 0
        ? new(Package, Name, [])
        : new(Package, AllocatedScope.Split('.')[0], [.. AllocatedScope.Split('.').Skip(1), Name]);
}
