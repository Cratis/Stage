// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Everything a channel downloaded for one package, as named byte parts. It is what the cache keeps, and <see cref="IPackageChannel.Open"/> verifies it
/// again every time it is read, so a cache holds nothing that is trusted.
/// </summary>
/// <param name="Parts">The parts by name; a name holds only letters, digits, dots, dashes and underscores.</param>
public sealed partial record PackageArtifact(IReadOnlyDictionary<string, byte[]> Parts)
{
    /// <summary>
    /// Tells whether a part name is safe to keep as a file name.
    /// </summary>
    /// <param name="name">The part name.</param>
    /// <returns>True when it is.</returns>
    public static bool IsValidName(string name) => PartName().IsMatch(name);

    /// <summary>
    /// Gets the sha256 digest of bytes in the form the lock uses.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The digest, such as <c lang="text">sha256:3f…</c>.</returns>
    public static string Sha256(ReadOnlySpan<byte> bytes) => ExtensionHashes.Of(bytes);

    /// <summary>
    /// Gets the SHA-512 integrity of bytes in the form npm uses.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The integrity, such as <c lang="text">sha512-…</c>.</returns>
    public static string Sha512Integrity(ReadOnlySpan<byte> bytes) => "sha512-" + Convert.ToBase64String(SHA512.HashData(bytes));

    [GeneratedRegex("^[A-Za-z0-9._-]{1,200}$")]
    private static partial Regex PartName();
}

/// <summary>
/// A package whose bytes have been checked against the digest the lock pins.
/// </summary>
/// <param name="Archives">The tar archives holding the files, in order.</param>
/// <param name="ArchiveRoot">The folder in the archives that is the package.</param>
/// <param name="Signatures">The signatures found.</param>
/// <param name="CoversPackage">Whether a signature payload names exactly this package.</param>
public sealed record FetchedPackage(IReadOnlyList<byte[]> Archives, string ArchiveRoot, IReadOnlyList<PackageSignature> Signatures, Func<byte[], bool> CoversPackage);

/// <summary>
/// One way packages are published: an OCI registry, or npm for frontend-only packages. Both give the same package, with the same contract.
/// </summary>
public interface IPackageChannel
{
    /// <summary>
    /// Tells whether the channel serves a resolved reference.
    /// </summary>
    /// <param name="resolved">The resolved reference of a lock entry.</param>
    /// <returns>True when it does.</returns>
    bool Handles(string resolved);

    /// <summary>
    /// Downloads what a lock entry pins, exactly by its digest and never by a version range.
    /// </summary>
    /// <param name="entry">The <see cref="ExtensionLockEntry"/>.</param>
    /// <param name="cancellationToken">Cancelled when loading stops.</param>
    /// <returns>The <see cref="PackageArtifact"/>.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the package is not there or what is there is not what the lock pins.</exception>
    Task<PackageArtifact> Download(ExtensionLockEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies downloaded or cached parts against the lock entry.
    /// </summary>
    /// <param name="entry">The <see cref="ExtensionLockEntry"/>.</param>
    /// <param name="artifact">The <see cref="PackageArtifact"/>.</param>
    /// <returns>The <see cref="FetchedPackage"/>.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the parts do not have the digest the lock pins.</exception>
    FetchedPackage Open(ExtensionLockEntry entry, PackageArtifact artifact);
}
