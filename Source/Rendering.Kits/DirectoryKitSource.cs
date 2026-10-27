// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

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
        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

        // Recheck after opening: author mode does not promise an atomic filesystem snapshot.
        RefuseLinks(new FileInfo(fullPath), path);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return [.. output.ToArray()];
    }

    static bool IsLink(FileSystemInfo entry)
    {
        entry.Refresh();
        return entry.LinkTarget is not null || entry.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    static bool HasLinkedAncestor(FileSystemInfo entry)
    {
        if (IsLink(entry))
        {
            return true;
        }

        var parent = entry is DirectoryInfo directory ? directory.Parent : ((FileInfo)entry).Directory;
        return parent is not null && HasLinkedAncestor(parent);
    }

    static void RefuseLinks(FileSystemInfo entry, string path)
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
        else if (entry.Attributes.HasFlag(FileAttributes.Device))
        {
            yield return new(path, KitSourceEntryKind.Other, 0);
        }
        else
        {
            yield return new(path, KitSourceEntryKind.File, ((FileInfo)entry).Length);
        }
    }
}
