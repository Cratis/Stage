// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Declares a model type and its stable owner and enclosing declaration identities.
/// </summary>
/// <param name="Type">The declared type.</param>
/// <param name="Owner">The stable model owner identity.</param>
/// <param name="Scope">The enclosing type scope, or null to use <see cref="TypeName.EnclosingScope"/>. An explicitly supplied scope must agree with the structured type.</param>
/// <param name="ParentOwner">The enclosing declaration's owner, or an empty string when not supplied.</param>
public sealed record DeclarationSegment(TypeName Type, string Owner, string? Scope = null, string ParentOwner = "") : SymbolSegment
{
    /// <summary>
    /// Gets superclass and interface metadata, keyed by erased structured type names (for example,
    /// lib.Base for lib.Base&lt;T&gt;). Omitted/default metadata is unknown and prevents shortening.
    /// Use <see cref="KnownTypes{T}.None"/> only to explicitly prove there are no supertype edges.
    /// </summary>
    public KnownTypes<TypeName> Supertypes { get; init; }

    internal override IEnumerable<string> Fields => Type.Fields.Concat([Owner, Scope ?? string.Empty, ParentOwner])
        .Concat(Supertypes.IsKnown ? Supertypes.Values.SelectMany(type => type.Fields) : []);
}
