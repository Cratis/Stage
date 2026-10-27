// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Contains literal generated text, not a symbol or sentinel.
/// </summary>
/// <param name="Text">The generated text.</param>
public sealed record TextSegment(string Text) : SymbolSegment
{
    internal override IEnumerable<string> Fields => [Text];
}
