// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Keeps what channels downloaded, by digest. The cache holds bytes and nothing it holds is trusted: every read is verified again against the lock,
/// and an entry that no longer verifies is downloaded again.
/// </summary>
/// <param name="root">The folder the cache lives in.</param>
public sealed class ExtensionPackageCache(string root)
{
    /// <summary>
    /// Gets the folder Stage keeps by default, in the local application data folder.
    /// </summary>
    public static string DefaultRoot { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cratis", "Stage", "extensions");

    /// <summary>
    /// Gets the folder the cache lives in.
    /// </summary>
    public string Root => root;

    /// <summary>
    /// Reads what was kept for a digest.
    /// </summary>
    /// <param name="digest">The digest from the lock.</param>
    /// <returns>The <see cref="PackageArtifact"/>, or null when nothing is kept.</returns>
    public PackageArtifact? TryRead(string digest)
    {
        var folder = FolderOf(digest);
        if (!Directory.Exists(folder)) return null;

        var parts = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(folder))
        {
            var name = Path.GetFileName(file);
            if (!PackageArtifact.IsValidName(name) || name.EndsWith(".tmp", StringComparison.Ordinal)) continue;
            parts[name] = File.ReadAllBytes(file);
        }

        return parts.Count == 0 ? null : new(parts);
    }

    /// <summary>
    /// Keeps a verified download, replacing anything kept for the digest.
    /// </summary>
    /// <param name="digest">The digest from the lock.</param>
    /// <param name="artifact">The <see cref="PackageArtifact"/>.</param>
    /// <exception cref="ArgumentException">Thrown when the digest or a part name is not safe to use as a path.</exception>
    public void Store(string digest, PackageArtifact artifact)
    {
        var folder = FolderOf(digest);
        var staging = folder + "." + Guid.NewGuid().ToString("N") + ".tmp";
        Directory.CreateDirectory(staging);
        foreach (var (name, bytes) in artifact.Parts)
        {
            if (!PackageArtifact.IsValidName(name)) throw new ArgumentException($"'{name}' is not a valid part name.", nameof(artifact));
            File.WriteAllBytes(Path.Combine(staging, name), bytes);
        }

        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.Move(staging, folder);
    }

    string FolderOf(string digest)
    {
        if (!ExtensionHashes.IsWellFormed(digest)) throw new ArgumentException($"'{digest}' is not a sha256 digest.", nameof(digest));
        return Path.Combine(root, digest.Replace(':', '-'));
    }
}
