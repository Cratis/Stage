// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Stage.Host;

/// <summary>
/// A read-only file provider over one folder that does not serve anything reached through a symbolic link below that folder,
/// so a link cannot lead a request to a file outside the folder.
/// </summary>
/// <param name="root">The folder to serve.</param>
internal sealed class StageContainedFileProvider(string root) : IFileProvider, IDisposable
{
    readonly string _root = Path.GetFullPath(root);
    readonly PhysicalFileProvider _inner = new(root);

    // The walk below stops at the root, so a root that is itself a link would serve whatever it points at: serve nothing instead.
    readonly bool _rootIsLink = IsLink(root);

    /// <inheritdoc/>
    public IFileInfo GetFileInfo(string subpath)
    {
        var info = _inner.GetFileInfo(subpath);

        return _rootIsLink || !info.Exists || ContainsLink(info.PhysicalPath) ? new NotFoundFileInfo(subpath) : info;
    }

    /// <inheritdoc/>
    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    /// <inheritdoc/>
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    /// <inheritdoc/>
    public void Dispose() => _inner.Dispose();

    static bool IsLink(string path)
    {
        try
        {
            var trimmed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            return new DirectoryInfo(trimmed).LinkTarget is not null || new FileInfo(trimmed).LinkTarget is not null;
        }
        catch (Exception)
        {
            return true;
        }
    }

    bool ContainsLink(string? physicalPath)
    {
        if (physicalPath is null) return true;

        var current = new FileInfo(physicalPath);
        while (current is not null && !string.Equals(Path.TrimEndingDirectorySeparator(current.FullName), Path.TrimEndingDirectorySeparator(_root), StringComparison.Ordinal))
        {
            if (current.LinkTarget is not null) return true;
            current = current.Directory is { } directory ? new FileInfo(directory.FullName) : null;
        }

        return current is null;
    }
}
