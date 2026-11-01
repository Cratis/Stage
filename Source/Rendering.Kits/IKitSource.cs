// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Supplies kit entries without resolving or normalizing their paths.</summary>
public interface IKitSource
{
    /// <summary>Enumerates entries, including unsafe entries so the loader can reject them.</summary>
    /// <returns>The source entries.</returns>
    IEnumerable<KitSourceEntry> Enumerate();

    /// <summary>Reads an entry as immutable bytes.</summary>
    /// <param name="path">The exact enumerated path.</param>
    /// <returns>The entry bytes.</returns>
    ImmutableArray<byte> Read(string path);
}
