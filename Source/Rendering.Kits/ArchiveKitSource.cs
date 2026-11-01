// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Formats.Tar;

namespace Cratis.Stage.Rendering.Kits;

sealed class ArchiveKitSource(ImmutableArray<byte> archive) : IKitSource
{
    public IEnumerable<KitSourceEntry> Enumerate()
    {
        using var stream = new MemoryStream([.. archive], false);
        using var reader = new TarReader(stream);
        while (reader.GetNextEntry() is { } entry)
        {
            var kind = entry.EntryType switch
            {
                TarEntryType.RegularFile or TarEntryType.V7RegularFile => KitSourceEntryKind.File,
                TarEntryType.SymbolicLink or TarEntryType.HardLink or TarEntryType.Directory => KitSourceEntryKind.SymbolicLink,
                _ => KitSourceEntryKind.Other
            };
            yield return new(entry.Name, kind, entry.Length);
        }
    }

    public ImmutableArray<byte> Read(string path)
    {
        using var stream = new MemoryStream([.. archive], false);
        using var reader = new TarReader(stream);
        while (reader.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, path, StringComparison.Ordinal) && entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                using var output = new MemoryStream();
                entry.DataStream?.CopyTo(output);
                return [.. output.ToArray()];
            }
        }

        throw new FileNotFoundException("Archive file was not found.", path);
    }
}
