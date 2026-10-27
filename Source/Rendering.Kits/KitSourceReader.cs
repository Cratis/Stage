// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

static class KitSourceReader
{
    internal static Dictionary<string, ImmutableArray<byte>> Read(IKitSource source, KitLoadLimits limits, List<KitDiagnostic> diagnostics)
    {
        var files = new Dictionary<string, ImmutableArray<byte>>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        var count = 0;
        try
        {
            foreach (var entry in source.Enumerate())
            {
                var code = EntryCode(entry, paths);
                if (code is not null)
                {
                    diagnostics.Add(new(code, "Unsafe or unsupported source entry.", entry.Path));
                    continue;
                }

                if (entry.Kind == KitSourceEntryKind.Directory)
                {
                    continue;
                }

                if (++count > limits.MaxFiles || entry.Length < 0 || entry.Length > limits.MaxFileBytes || entry.Length > limits.MaxTotalBytes - total)
                {
                    diagnostics.Add(new(KitCodes.LoadLimit, "Kit load limit exceeded.", entry.Path));
                    continue;
                }

                var bytes = source.Read(entry.Path);
                if (bytes.IsDefault || bytes.Length != entry.Length || bytes.Length > limits.MaxFileBytes || bytes.Length > limits.MaxTotalBytes - total)
                {
                    diagnostics.Add(new(KitCodes.LoadLimit, "Source length changed or exceeds kit load limits.", entry.Path));
                    continue;
                }

                total += bytes.Length;
                files.Add(entry.Path, bytes);
            }
        }
        catch (KitSourceException exception)
        {
            diagnostics.Add(new(exception.Code, exception.Message, exception.EntryPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new(KitCodes.LoadLimit, exception.Message, "$"));
        }

        return files;
    }

    static string? EntryCode(KitSourceEntry entry, HashSet<string> paths)
    {
        if (entry.Kind == KitSourceEntryKind.SymbolicLink)
        {
            return KitCodes.SymbolicLink;
        }

        if (entry.Kind != KitSourceEntryKind.File && entry.Kind != KitSourceEntryKind.Directory)
        {
            return KitCodes.LoadLimit;
        }

        return KitPath.Validate(entry.Path) ?? (paths.Add(entry.Path) ? null : KitCodes.DuplicatePath);
    }
}
