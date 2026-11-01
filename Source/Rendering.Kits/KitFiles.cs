// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>An immutable ordinal map of kit-relative paths to immutable bytes.</summary>
/// <param name="files">Validated files.</param>
public sealed class KitFiles(IEnumerable<KeyValuePair<string, ImmutableArray<byte>>> files)
{
    readonly ImmutableSortedDictionary<string, ImmutableArray<byte>> _files = files.ToImmutableSortedDictionary(StringComparer.Ordinal);

    /// <summary>Gets the paths in ordinal order.</summary>
    public IEnumerable<string> Paths => _files.Keys;

    /// <summary>Gets the immutable bytes for a path.</summary>
    /// <param name="path">Exact kit-relative path.</param>
    /// <returns>The file bytes.</returns>
    public ImmutableArray<byte> this[string path] => _files[path];
}
