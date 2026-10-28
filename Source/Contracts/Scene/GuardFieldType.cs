// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Guards;

namespace Cratis.Stage.Contracts.Scene;

internal enum GuardFieldKind
{
    Unknown,
    Text,
    Number,
    Boolean,
    DateTime,
    Identifier,
    Enumeration,
    Composite,
    Collection
}

/// <summary>The resolved authored type, shared by guard admission and serialization.</summary>
internal sealed record GuardFieldType(GuardFieldKind Kind, bool IsOptional = false, IReadOnlyList<string>? Members = null, string? Reason = null, IReadOnlyList<int>? MemberNumbers = null)
{
    internal static GuardFieldType Unknown(string reason) => new(GuardFieldKind.Unknown, Reason: reason);

    internal bool Admits(GuardOperator operation, object? literal) => Kind != GuardFieldKind.Unknown &&
        (operation is not (GuardOperator.Contains or GuardOperator.StartsWith) || (Kind == GuardFieldKind.Text && literal is string));
}
