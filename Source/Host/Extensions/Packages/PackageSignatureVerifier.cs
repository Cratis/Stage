// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// One signature found for a package.
/// </summary>
/// <param name="Payload">The bytes that were signed.</param>
/// <param name="Signature">The ECDSA signature over the SHA-256 of the payload, in DER form.</param>
public sealed record PackageSignature(byte[] Payload, byte[] Signature);

/// <summary>
/// What verifying a package's signatures found.
/// </summary>
public enum SignatureVerdict
{
    /// <summary>A signature by one of the publisher's keys covers exactly this package.</summary>
    Valid,

    /// <summary>The package carries no signature at all.</summary>
    Unsigned,

    /// <summary>There are signatures, but none by the publisher's keys that covers this package.</summary>
    Invalid
}

/// <summary>
/// Verifies a package's signatures against the keys of the publisher the lock records. Keys are ECDSA P-256 public keys, as <c lang="text">cosign generate-key-pair</c> writes them.
/// A signature only counts when what it signed names this package, so a valid signature of another package, or of another version, is never accepted.
/// </summary>
public static class PackageSignatureVerifier
{
    /// <summary>
    /// The annotation of a registry signature layer that holds the signature.
    /// </summary>
    public const string SignatureAnnotation = "dev.cosignproject.cosign/signature";

    /// <summary>
    /// Verifies the signatures of a package.
    /// </summary>
    /// <param name="signatures">The signatures that were found.</param>
    /// <param name="keys">The public keys of the publisher, as PEM or the base64 body of one.</param>
    /// <param name="coversPackage">Whether the payload names exactly the package being loaded.</param>
    /// <returns>The <see cref="SignatureVerdict"/>.</returns>
    public static SignatureVerdict Verify(IReadOnlyList<PackageSignature> signatures, IReadOnlyList<string> keys, Func<byte[], bool> coversPackage)
    {
        if (signatures.Count == 0) return SignatureVerdict.Unsigned;

        var parsed = keys.Select(Parse).OfType<ECDsa>().ToList();
        try
        {
            foreach (var signature in signatures)
            {
                if (!coversPackage(signature.Payload)) continue;
                if (parsed.Any(key => Verifies(key, signature))) return SignatureVerdict.Valid;
            }

            return SignatureVerdict.Invalid;
        }
        finally
        {
            parsed.ForEach(key => key.Dispose());
        }
    }

    /// <summary>
    /// Tells whether a registry signature payload names a package by its digest.
    /// </summary>
    /// <param name="payload">The simple signing payload.</param>
    /// <param name="digest">The digest of the package.</param>
    /// <returns>True when the payload is for exactly that digest.</returns>
    public static bool PayloadNamesDigest(byte[] payload, string digest)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("critical", out var critical)
                && critical.TryGetProperty("image", out var image)
                && image.TryGetProperty("docker-manifest-digest", out var named)
                && string.Equals(named.GetString(), digest, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool Verifies(ECDsa key, PackageSignature signature)
    {
        try
        {
            return key.VerifyData(signature.Payload, signature.Signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    static ECDsa? Parse(string key)
    {
        try
        {
            var ecdsa = ECDsa.Create();
            if (key.Contains("-----BEGIN", StringComparison.Ordinal)) ecdsa.ImportFromPem(key);
            else ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key.Trim()), out _);
            return ecdsa.KeySize == 256 ? ecdsa : Discard(ecdsa);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    static ECDsa? Discard(ECDsa key)
    {
        key.Dispose();
        return null;
    }
}
