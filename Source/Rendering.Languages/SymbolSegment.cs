// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Represents one structured segment. Only the pack's declared segment kinds can derive from this record.
/// </summary>
public abstract record SymbolSegment
{
    private protected SymbolSegment()
    {
    }

    internal abstract IEnumerable<string> Fields { get; }
}
