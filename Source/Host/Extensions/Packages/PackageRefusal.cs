// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Why a package was refused.
/// </summary>
public enum PackageRefusalReason
{
    /// <summary>The deployment and the project do not allow the package or its publisher.</summary>
    NotAllowed,

    /// <summary>The package could not be fetched from where the lock says it is.</summary>
    NotFound,

    /// <summary>What was fetched does not have the digest the lock pins.</summary>
    DigestMismatch,

    /// <summary>The package's contract is not the contract the lock pins.</summary>
    ContractMismatch,

    /// <summary>The package is not signed, or the signature is not by the publisher the lock records.</summary>
    Unsigned,

    /// <summary>A signature is present but does not verify.</summary>
    SignatureInvalid,

    /// <summary>The package, the lock entry or the contract is not well formed, or a file in it is not safe to unpack.</summary>
    Malformed,

    /// <summary>The package is larger than the deployment allows.</summary>
    TooLarge,

    /// <summary>The package requires something this Stage does not offer.</summary>
    RequirementsNotMet
}

/// <summary>
/// A package that is not loaded, and why.
/// </summary>
/// <param name="Reference">The reference the model names.</param>
/// <param name="Reason">The <see cref="PackageRefusalReason"/>.</param>
/// <param name="Details">What is wrong, for a person to read.</param>
public sealed record PackageRefusal(string Reference, PackageRefusalReason Reason, string Details)
{
    /// <summary>
    /// The capability a refused package is reported under in the admission.
    /// </summary>
    public const string Capability = "ExtensionPackage";

    /// <summary>
    /// Describes the refusal as an issue the admission reports.
    /// </summary>
    /// <returns>The <see cref="StageUnsupportedIssue"/>.</returns>
    public StageUnsupportedIssue ToIssue() => new(Capability, Reference, $"{Reference}: {Reason}: {Details}");
}

/// <summary>
/// Thrown while a package is read when it must be refused.
/// </summary>
/// <param name="reason">The <see cref="PackageRefusalReason"/>.</param>
/// <param name="details">What is wrong.</param>
public sealed class PackageRefusedException(PackageRefusalReason reason, string details) : Exception(details)
{
    /// <summary>
    /// Gets the reason the package is refused.
    /// </summary>
    public PackageRefusalReason Reason { get; } = reason;
}
