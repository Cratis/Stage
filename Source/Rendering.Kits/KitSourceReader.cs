// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

sealed class KitSourceReader(IKitSource source, KitLoadLimits limits, List<KitDiagnostic> diagnostics)
{
    readonly Dictionary<string, ImmutableArray<byte>> _files = new(StringComparer.Ordinal);
    readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
    long _total;
    int _count;

    internal static Dictionary<string, ImmutableArray<byte>> Read(IKitSource source, KitLoadLimits limits, List<KitDiagnostic> diagnostics) => new KitSourceReader(source, limits, diagnostics).ReadSource();

    static bool LengthChanged(ImmutableArray<byte> bytes, KitSourceEntry entry) => bytes.IsDefault || bytes.Length != entry.Length;

    string? EntryCode(KitSourceEntry entry)
    {
        if (entry.Kind == KitSourceEntryKind.SymbolicLink)
        {
            return KitCodes.SymbolicLink;
        }

        if (entry.Kind != KitSourceEntryKind.File && entry.Kind != KitSourceEntryKind.Directory)
        {
            return KitCodes.LoadLimit;
        }

        return KitPath.Validate(entry.Path) ?? (_paths.Add(entry.Path) ? null : KitCodes.DuplicatePath);
    }

    Dictionary<string, ImmutableArray<byte>> ReadSource()
    {
        try
        {
            foreach (var entry in source.Enumerate())
            {
                var code = EntryCode(entry);
                if (code is not null)
                {
                    diagnostics.Add(new(code, "Unsafe or unsupported source entry.", entry.Path));
                    continue;
                }

                if (entry.Kind == KitSourceEntryKind.File)
                {
                    ReadFile(entry);
                }
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

        return _files;
    }

    void ReadFile(KitSourceEntry entry)
    {
        if (++_count > limits.MaxFiles || ExceedsBudget(entry.Length))
        {
            diagnostics.Add(new(KitCodes.LoadLimit, "Kit load limit exceeded.", entry.Path));
            return;
        }

        var bytes = source.Read(entry.Path);
        if (LengthChanged(bytes, entry) || ExceedsBudget(bytes.Length))
        {
            diagnostics.Add(new(KitCodes.LoadLimit, "Source length changed or exceeds kit load limits.", entry.Path));
            return;
        }

        _total += bytes.Length;
        _files.Add(entry.Path, bytes);
    }

    bool ExceedsBudget(long length) => length < 0 || length > limits.MaxFileBytes || length > limits.MaxTotalBytes - _total;
}
