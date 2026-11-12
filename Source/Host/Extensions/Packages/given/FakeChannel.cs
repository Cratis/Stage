// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages.given;

/// <summary>
/// A channel that serves one archive from memory and counts what was asked of it. Its digest is the sha256 of the archive.
/// </summary>
public sealed class FakeChannel(byte[] archive, string root, IReadOnlyList<PackageSignature> signatures) : IPackageChannel
{
    public int Downloads { get; private set; }

    public int Opens { get; private set; }

    public byte[] Archive { get; set; } = archive;

    public bool Handles(string resolved) => true;

    public Task<PackageArtifact> Download(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        Downloads++;
        return Task.FromResult(new PackageArtifact(new Dictionary<string, byte[]> { ["archive"] = Archive }));
    }

    public FetchedPackage Open(ExtensionLockEntry entry, PackageArtifact artifact)
    {
        Opens++;
        var bytes = artifact.Parts["archive"];
        if (ExtensionHashes.Of(bytes) != entry.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, "The archive does not have the locked digest.");
        return new([bytes], root, signatures, payload => PackageSignatureVerifier.PayloadNamesDigest(payload, entry.Digest));
    }
}
