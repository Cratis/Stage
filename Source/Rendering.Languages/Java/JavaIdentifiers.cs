// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Defines the reserved and contextual names protected by java@1, pinned to JDK 21.
/// </summary>
public static class JavaIdentifiers
{
    static readonly FrozenSet<string> _reserved =
        ("abstract assert boolean break byte case catch char class const continue default do double else enum extends " +
         "final finally float for goto if implements import instanceof int interface long native new package private " +
         "protected public return short static strictfp super switch synchronized this throw throws transient try void " +
         "volatile while true false null _ exports module open opens provides requires to transitive uses with record " +
         "sealed permits var yield when non-sealed").Split(' ').ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a name is reserved or contextual in the java@1 naming policy.
    /// </summary>
    /// <param name="name">The case-sensitive name.</param>
    /// <returns>Whether generated identifiers must avoid this name.</returns>
    public static bool IsReserved(string name) => _reserved.Contains(name);
}
