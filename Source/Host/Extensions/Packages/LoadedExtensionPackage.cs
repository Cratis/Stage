// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// An extension package that was fetched by its locked digest, allowed, verified and admitted.
/// </summary>
/// <param name="Lock">The <see cref="ExtensionLockEntry"/> it was loaded for.</param>
/// <param name="Contract">The <see cref="ExtensionContract"/> read from the package.</param>
/// <param name="Contents">The <see cref="PackageContents"/>.</param>
/// <param name="Channel">The channel it came from, <c lang="text">oci</c> or <c lang="text">npm</c>.</param>
/// <param name="Signed">Whether a signature by the publisher's key was verified.</param>
/// <param name="BackendUnpacker">Unpacks the backend into a folder from the verified bytes, or null when the package was not loaded by a loader.</param>
public sealed record LoadedExtensionPackage(ExtensionLockEntry Lock, ExtensionContract Contract, PackageContents Contents, string Channel, bool Signed, Action<string>? BackendUnpacker = null)
{
    /// <summary>
    /// Gets the locked digest, which is what identifies the package everywhere.
    /// </summary>
    public string Digest => Lock.Digest;

    /// <summary>
    /// Gets the identifier of the package in addresses, the digest without its algorithm.
    /// </summary>
    public string Id => Lock.Digest["sha256:".Length..];

    /// <summary>
    /// Gets whether the package has a backend to run.
    /// </summary>
    public bool HasBackend => Contract.Backend is not null;

    /// <summary>
    /// Gets whether the package ships a frontend.
    /// </summary>
    public bool HasFrontend => Contract.Frontend is not null;

    /// <summary>
    /// Unpacks the backend into a folder, for whatever runs it. The bytes are verified against the locked digest again first, so what is unpacked is what was admitted.
    /// </summary>
    /// <param name="folder">The folder to unpack into.</param>
    /// <exception cref="InvalidOperationException">Thrown when the package has no backend or was not loaded by a loader.</exception>
    /// <exception cref="PackageRefusedException">Thrown when the cached bytes no longer verify.</exception>
    public void UnpackBackend(string folder)
    {
        if (!HasBackend || BackendUnpacker is null) throw new InvalidOperationException($"The package {Lock.Reference} has no backend to unpack.");
        BackendUnpacker(folder);
    }
}

/// <summary>
/// The outcome of loading the packages a lock pins.
/// </summary>
/// <param name="Loaded">The packages that are loaded.</param>
/// <param name="Refusals">The packages that are not, and why.</param>
/// <param name="Contracts">Where the contracts of the loaded packages are kept for the compiler, or null when nothing was loaded.</param>
public sealed record ExtensionPackageSet(IReadOnlyList<LoadedExtensionPackage> Loaded, IReadOnlyList<PackageRefusal> Refusals, ExtensionCache? Contracts = null)
{
    /// <summary>
    /// Gets the empty outcome, of a model that names no extension.
    /// </summary>
    public static ExtensionPackageSet Empty { get; } = new([], []);

    /// <summary>
    /// Finds a loaded package by the reference a model names.
    /// </summary>
    /// <param name="reference">The reference as the model writes it.</param>
    /// <returns>The package, or null.</returns>
    public LoadedExtensionPackage? FindByReference(string reference) => Loaded.FirstOrDefault(package => package.Lock.Reference == reference);

    /// <summary>
    /// Finds a loaded package by its digest.
    /// </summary>
    /// <param name="digest">The locked digest.</param>
    /// <returns>The package, or null.</returns>
    public LoadedExtensionPackage? FindByDigest(string digest) => Loaded.FirstOrDefault(package => package.Digest == digest);
}
