// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Marks the location of a file's language-pack-generated import block.
/// </summary>
public sealed record ImportsSegment : SymbolSegment
{
    internal override IEnumerable<string> Fields => [];
}
