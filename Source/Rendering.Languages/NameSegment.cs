// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Requests the simple name of a type, optionally bound to a model owner.
/// </summary>
/// <param name="Type">The type whose name is requested.</param>
/// <param name="Owner">The stable model owner, or an empty string when not supplied.</param>
public sealed record NameSegment(TypeName Type, string Owner = "") : SymbolSegment
{
    internal override IEnumerable<string> Fields => Type.Fields.Append(Owner);
}
