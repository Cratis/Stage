// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Describes a file as ordered structured segments, with no sentinel encoding.
/// </summary>
/// <param name="Id">The stable file identity.</param>
/// <param name="Package">The file's package, or an empty string for the unnamed package.</param>
/// <param name="Segments">The ordered file segments.</param>
public sealed record SymbolFile(string Id, string Package, IReadOnlyList<SymbolSegment> Segments);
