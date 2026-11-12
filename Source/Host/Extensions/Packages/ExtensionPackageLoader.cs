// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Loads the packages a lock pins. Exactly the locked digests are fetched, never a version range; a package is read without running anything, and is only loaded
/// once it is allowed, has the pinned digest and contract, carries a signature by the recorded publisher's key, and its contents are admitted. Anything else is
/// refused, with the reason, before any of it can run.
/// </summary>
/// <param name="channels">The <see cref="IPackageChannel"/>s packages come from.</param>
/// <param name="cache">The <see cref="ExtensionPackageCache"/>.</param>
/// <param name="trust">The <see cref="ExtensionTrustPolicy"/>.</param>
/// <param name="admission">The <see cref="ExtensionPackageAdmission"/>.</param>
/// <param name="options">The <see cref="ExtensionPackageOptions"/>.</param>
/// <param name="contracts">Where restored contracts are kept for the compiler.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class ExtensionPackageLoader(
    IEnumerable<IPackageChannel> channels,
    ExtensionPackageCache cache,
    ExtensionTrustPolicy trust,
    ExtensionPackageAdmission admission,
    ExtensionPackageOptions options,
    ExtensionCache contracts,
    ILogger logger)
{
    /// <summary>
    /// Loads every package in the lock.
    /// </summary>
    /// <param name="extensionLock">The <see cref="ExtensionLock"/>.</param>
    /// <param name="cancellationToken">Cancelled when loading stops.</param>
    /// <returns>The <see cref="ExtensionPackageSet"/>.</returns>
    public async Task<ExtensionPackageSet> Load(ExtensionLock extensionLock, CancellationToken cancellationToken = default)
    {
        var loaded = new List<LoadedExtensionPackage>();
        var refusals = new List<PackageRefusal>();
        if (!options.RequireSignatures && extensionLock.Entries.Length > 0) ExtensionPackageLog.SignaturesNotRequired(logger);

        foreach (var entry in extensionLock.Entries)
        {
            try
            {
                var package = await LoadOne(entry, cancellationToken);
                loaded.Add(package);
                ExtensionPackageLog.Loaded(logger, entry.Reference, entry.Version, entry.Digest, package.Channel, package.SignatureVerified);
            }
            catch (PackageRefusedException refused)
            {
                refusals.Add(new(entry.Reference, refused.Reason, refused.Message));
                ExtensionPackageLog.Refused(logger, entry.Reference, refused.Reason, refused.Message);
            }
        }

        return new(loaded, refusals, contracts);
    }

    static bool Verify(FetchedPackage fetched, PackageTrust decision)
    {
        if (!decision.RequireSignature && decision.Keys.Count == 0) return false;

        var verdict = PackageSignatureVerifier.Verify(fetched.Signatures, decision.Keys, fetched.CoversPackage);
        return verdict switch
        {
            SignatureVerdict.Valid => true,
            SignatureVerdict.NotSigned when !decision.RequireSignature => false,
            SignatureVerdict.NotSigned => throw new PackageRefusedException(PackageRefusalReason.NotSigned, "The package is not signed."),
            _ => throw new PackageRefusedException(PackageRefusalReason.SignatureInvalid, "No signature by a key of the publisher the lock records covers this package.")
        };
    }

    async Task<LoadedExtensionPackage> LoadOne(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        var decision = trust.Evaluate(entry);
        if (!decision.Allowed) throw new PackageRefusedException(PackageRefusalReason.NotAllowed, decision.Refusal);

        if (!ExtensionHashes.IsWellFormed(entry.Digest) || !ExtensionHashes.IsWellFormed(entry.ContractHash) || entry.Publisher.Length == 0)
        {
            throw new PackageRefusedException(PackageRefusalReason.Malformed, "The lock entry needs a sha256 digest, a sha256 contract hash and a publisher.");
        }

        var channel = channels.FirstOrDefault(candidate => candidate.Handles(entry.Resolved))
            ?? throw new PackageRefusedException(PackageRefusalReason.Malformed, $"No channel serves '{entry.Resolved}'.");

        var fetched = await Fetch(channel, entry, cancellationToken);
        var signed = Verify(fetched, decision);
        var streams = fetched.Archives.Select(archive => (Stream)new MemoryStream(archive, writable: false)).ToList();
        var contents = new PackageArchiveReader(options.MaxPackageBytes, options.MaxPackageFiles).Read(streams, fetched.ArchiveRoot);
        var contract = admission.Admit(entry, contents);

        contracts.Store(entry.Digest, contents.Contract!);
        return new(entry, contract, contents, channel is NpmPackageChannel ? "npm" : "oci", signed, folder => UnpackBackend(channel, entry, folder));
    }

    void UnpackBackend(IPackageChannel channel, ExtensionLockEntry entry, string folder)
    {
        var artifact = cache.TryRead(entry.Digest) ?? throw new PackageRefusedException(PackageRefusalReason.NotFound, "The package is no longer in the cache.");
        var fetched = channel.Open(entry, artifact);
        new PackageArchiveReader(options.MaxPackageBytes, options.MaxPackageFiles).ExtractBackend(fetched.Archives.Select(archive => (Stream)new MemoryStream(archive, writable: false)), fetched.ArchiveRoot, folder);
    }

    async Task<FetchedPackage> Fetch(IPackageChannel channel, ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        if (cache.TryRead(entry.Digest) is { } kept)
        {
            try
            {
                return channel.Open(entry, kept);
            }
            catch (PackageRefusedException refused)
            {
                ExtensionPackageLog.CacheRejected(logger, entry.Digest, refused.Message);
            }
        }

        ExtensionPackageLog.Downloading(logger, entry.Reference, entry.Version, entry.Digest);
        var artifact = await channel.Download(entry, cancellationToken);
        var fetched = channel.Open(entry, artifact);
        cache.Store(entry.Digest, artifact);
        return fetched;
    }
}
