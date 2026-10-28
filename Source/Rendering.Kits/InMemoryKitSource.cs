// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A byte-backed source that preserves entry order and duplicate names for validation.</summary>
/// <param name="files">The files in enumeration order.</param>
public sealed class InMemoryKitSource(IEnumerable<KeyValuePair<string, ImmutableArray<byte>>> files) : IKitSource
{
    readonly ImmutableArray<KeyValuePair<string, ImmutableArray<byte>>> _files = [.. files];

    /// <inheritdoc/>
    public IEnumerable<KitSourceEntry> Enumerate() => _files.Select(file => new KitSourceEntry(file.Key, KitSourceEntryKind.File, file.Value.Length));

    /// <inheritdoc/>
    public ImmutableArray<byte> Read(string path) => _files.First(file => string.Equals(file.Key, path, StringComparison.Ordinal)).Value;
}
