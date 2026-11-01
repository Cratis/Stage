// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Formats.Tar;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A directory source that never follows symbolic links, including root and ancestor links.</summary>
/// <param name="root">The kit root directory.</param>
public sealed class DirectoryKitSource(string root) : IKitSource
{
    readonly string _root = Path.GetFullPath(root);

    /// <inheritdoc/>
    public IEnumerable<KitSourceEntry> Enumerate() => EnumerateDirectory(new DirectoryInfo(_root));

    /// <inheritdoc/>
    public ImmutableArray<byte> Read(string path)
    {
        if (KitPath.Validate(path) is not null)
        {
            throw new KitSourceException(KitPath.Validate(path)!, path);
        }

        var fullPath = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        RefuseLinks(new FileInfo(fullPath), path);
        if (!IsRegularFile(new FileInfo(fullPath)))
        {
            throw new KitSourceException(KitCodes.LoadLimit, path);
        }

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

        // Recheck after opening: author mode does not promise an atomic filesystem snapshot.
        RefuseLinks(new FileInfo(fullPath), path);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return [.. output.ToArray()];
    }

    // On Unix, FIFOs, sockets and device nodes carry no FileAttributes.Device and report no length; opening a FIFO
    // blocks. The tar writer reads the real file type from the file system without opening anything but regular files.
    static bool IsRegularFile(FileInfo file)
    {
        // Windows has no FIFOs or device nodes in the file system and its TarWriter refuses some valid files (such as an
        // empty read-only file), so only Unix probes.
        if (OperatingSystem.IsWindows() || !file.Exists || file.Length > 0)
        {
            // A missing file fails when it is opened, which reports it as unreadable.
            return true;
        }

        try
        {
            using var probe = new MemoryStream();
            using (var writer = new TarWriter(probe, TarEntryFormat.Pax, true))
            {
                writer.WriteEntry(file.FullName, "probe");
            }

            probe.Position = 0;
            using var reader = new TarReader(probe);
            return reader.GetNextEntry()?.EntryType == TarEntryType.RegularFile;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // A file type the tar writer cannot represent (such as a socket) is not a regular file.
            return false;
        }
    }

    static bool IsLink(FileSystemInfo entry)
    {
        entry.Refresh();
        return entry.LinkTarget is not null || entry.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    // Only the kit root and the components below it are checked: links above the root (such as /var or /tmp on macOS)
    // belong to the host's filesystem layout, not to the kit.
    bool HasLinkedAncestor(FileSystemInfo entry)
    {
        if (IsLink(entry))
        {
            return true;
        }

        if (string.Equals(Path.TrimEndingDirectorySeparator(entry.FullName), Path.TrimEndingDirectorySeparator(_root), StringComparison.Ordinal))
        {
            return false;
        }

        var parent = entry is DirectoryInfo directory ? directory.Parent : ((FileInfo)entry).Directory;
        return parent is not null && HasLinkedAncestor(parent);
    }

    void RefuseLinks(FileSystemInfo entry, string path)
    {
        if (HasLinkedAncestor(entry))
        {
            throw new KitSourceException(KitCodes.SymbolicLink, path);
        }
    }

    IEnumerable<KitSourceEntry> EnumerateDirectory(DirectoryInfo directory)
    {
        var relative = Path.GetRelativePath(_root, directory.FullName).Replace(Path.DirectorySeparatorChar, '/');
        if (HasLinkedAncestor(directory))
        {
            yield return new(relative, KitSourceEntryKind.SymbolicLink, 0);
            yield break;
        }

        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            foreach (var descendant in EnumerateEntry(entry))
            {
                yield return descendant;
            }
        }
    }

    IEnumerable<KitSourceEntry> EnumerateEntry(FileSystemInfo entry)
    {
        var path = Path.GetRelativePath(_root, entry.FullName).Replace(Path.DirectorySeparatorChar, '/');
        if (IsLink(entry))
        {
            yield return new(path, KitSourceEntryKind.SymbolicLink, 0);
        }
        else if (entry is DirectoryInfo child)
        {
            yield return new(path, KitSourceEntryKind.Directory, 0);
            foreach (var descendant in EnumerateDirectory(child))
            {
                yield return descendant;
            }
        }
        else if (entry.Attributes.HasFlag(FileAttributes.Device) || !IsRegularFile((FileInfo)entry))
        {
            yield return new(path, KitSourceEntryKind.Other, 0);
        }
        else
        {
            yield return new(path, KitSourceEntryKind.File, ((FileInfo)entry).Length);
        }
    }
}
