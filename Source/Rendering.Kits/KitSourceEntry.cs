// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A source entry and its advertised byte length.</summary>
/// <param name="Path">Kit-relative path.</param>
/// <param name="Kind">Entry kind.</param>
/// <param name="Length">File length in bytes.</param>
public sealed record KitSourceEntry(string Path, KitSourceEntryKind Kind, long Length);
