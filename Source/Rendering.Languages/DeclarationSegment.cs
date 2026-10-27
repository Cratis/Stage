// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Declares a model type and its stable owner and enclosing declaration identities.
/// </summary>
/// <param name="Type">The declared type.</param>
/// <param name="Owner">The stable model owner identity.</param>
/// <param name="Scope">The enclosing type scope, or an empty string for a top-level declaration.</param>
/// <param name="ParentOwner">The enclosing declaration's owner, or an empty string when not supplied.</param>
public sealed record DeclarationSegment(TypeName Type, string Owner, string Scope = "", string ParentOwner = "") : SymbolSegment
{
    internal override IEnumerable<string> Fields => Type.Fields.Concat([Owner, Scope, ParentOwner]);
}
