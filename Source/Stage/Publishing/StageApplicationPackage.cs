// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Publishing;

/// <summary>
/// A versioned, publishable application package: the manifest, the model files under <c language="shell">model/</c>
/// and a <c language="shell">Dockerfile</c> that bakes the package into a Stage image.
/// </summary>
public sealed class StageApplicationPackage
{
    internal StageApplicationPackage(PackageManifest manifest, string manifestJson, IReadOnlyDictionary<string, byte[]> files)
    {
        Manifest = manifest;
        ManifestJson = manifestJson;
        Files = files;
    }

    /// <summary>
    /// Gets the manifest.
    /// </summary>
    public PackageManifest Manifest { get; }

    /// <summary>
    /// Gets the manifest as written to <c language="shell">manifest.json</c>.
    /// </summary>
    public string ManifestJson { get; }

    /// <summary>
    /// Gets every file of the package by its relative path: <c language="shell">manifest.json</c>, <c language="shell">Dockerfile</c> and <c language="shell">model/...</c>.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]> Files { get; }

    /// <summary>
    /// Packs the files into a <c language="shell">.tar.gz</c> archive. The same package always gives the same bytes: entries are
    /// sorted, owned by uid and gid 0 and carry a fixed modification time.
    /// </summary>
    /// <returns>The archive.</returns>
    public byte[] ToArchive() => PackageArchive.Write(Files);
}
