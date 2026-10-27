// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// References a type from an optional lexical owner, without deciding its spelling yet.
/// </summary>
/// <param name="Type">The referenced type.</param>
/// <param name="Owner">The target model owner, or an empty string when not supplied.</param>
/// <param name="LexicalOwner">The lexical declaration owner, or an empty string when not supplied.</param>
public sealed record ReferenceSegment(TypeName Type, string Owner = "", string LexicalOwner = "") : SymbolSegment
{
    internal override IEnumerable<string> Fields => Type.Fields.Concat([Owner, LexicalOwner]);
}
