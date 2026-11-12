// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Serves full extensions, which are OCI artifacts: a manifest whose tar layers hold the package under <c lang="text">extension/</c>, with the contract at
/// <c lang="text">extension/contract.play</c>. The manifest is fetched by the digest in the lock and never by a tag, so what runs is what was locked. A signature is read from the
/// <c lang="text">sha256-&lt;digest&gt;.sig</c> artifact that <c lang="text">cosign sign --key</c> pushes next to it.
/// </summary>
/// <param name="registry">The <see cref="OciRegistryClient"/>.</param>
/// <param name="options">The <see cref="ExtensionPackageOptions"/>.</param>
public sealed partial class OciPackageChannel(OciRegistryClient registry, ExtensionPackageOptions options) : IPackageChannel
{
    /// <summary>
    /// The folder in the layers that holds the package.
    /// </summary>
    public const string Root = "extension";

    const string ManifestPart = "manifest.json";
    const string SignatureManifestPart = "signature-manifest.json";

    [GeneratedRegex("^sha256:[0-9a-f]{64}$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DigestPattern { get; }

    /// <inheritdoc/>
    public bool Handles(string resolved) => !resolved.StartsWith("npm:", StringComparison.Ordinal);

    /// <inheritdoc/>
    public async Task<PackageArtifact> Download(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        var reference = OciReference.Parse(entry.Resolved);
        var parts = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        var manifest = await registry.Get(reference, "manifests", entry.Digest, 1024 * 1024, cancellationToken)
            ?? throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The registry does not have {entry.Resolved}@{entry.Digest}.");
        if (PackageArtifact.Sha256(manifest) != entry.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, $"The registry answered with a manifest whose digest is {PackageArtifact.Sha256(manifest)}, not the locked {entry.Digest}.");
        parts[ManifestPart] = manifest;

        foreach (var layer in Layers(manifest).Where(layer => layer.IsArchive))
        {
            parts[BlobPart("blob", layer.Digest)] = await registry.Get(reference, "blobs", layer.Digest, options.MaxPackageBytes, cancellationToken)
                ?? throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The registry does not have the layer {layer.Digest}.");
        }

        var signature = await registry.Get(reference, "manifests", $"sha256-{entry.Digest["sha256:".Length..]}.sig", 1024 * 1024, cancellationToken);
        if (signature is not null)
        {
            parts[SignatureManifestPart] = signature;
            foreach (var layer in Layers(signature).Where(layer => layer.Signature is not null))
            {
                if (await registry.Get(reference, "blobs", layer.Digest, 64 * 1024, cancellationToken) is { } payload) parts[BlobPart("signature-blob", layer.Digest)] = payload;
            }
        }

        return new(parts);
    }

    /// <inheritdoc/>
    public FetchedPackage Open(ExtensionLockEntry entry, PackageArtifact artifact)
    {
        if (!artifact.Parts.TryGetValue(ManifestPart, out var manifest)) throw new PackageRefusedException(PackageRefusalReason.NotFound, "The package was not downloaded.");
        if (PackageArtifact.Sha256(manifest) != entry.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, $"The manifest's digest is {PackageArtifact.Sha256(manifest)}, not the locked {entry.Digest}.");

        var archives = new List<byte[]>();
        foreach (var layer in Layers(manifest).Where(layer => layer.IsArchive))
        {
            if (!artifact.Parts.TryGetValue(BlobPart("blob", layer.Digest), out var blob)) throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The layer {layer.Digest} was not downloaded.");
            if (PackageArtifact.Sha256(blob) != layer.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, $"A layer is not {layer.Digest}.");
            archives.Add(blob);
        }

        if (archives.Count == 0) throw new PackageRefusedException(PackageRefusalReason.Malformed, "The artifact has no layer holding the package.");

        var signatures = new List<PackageSignature>();
        if (artifact.Parts.TryGetValue(SignatureManifestPart, out var signatureManifest))
        {
            foreach (var layer in Layers(signatureManifest).Where(layer => layer.Signature is not null))
            {
                if (artifact.Parts.TryGetValue(BlobPart("signature-blob", layer.Digest), out var payload) && PackageArtifact.Sha256(payload) == layer.Digest) signatures.Add(new(payload, layer.Signature!));
            }
        }

        return new(archives, Root, signatures, payload => PackageSignatureVerifier.PayloadNamesDigest(payload, entry.Digest));
    }

    static string BlobPart(string prefix, string digest) => $"{prefix}-{digest["sha256:".Length..]}";

    static List<Layer> Layers(byte[] manifest)
    {
        try
        {
            using var document = JsonDocument.Parse(manifest);
            var root = document.RootElement;
            if (root.TryGetProperty("manifests", out _)) throw new PackageRefusedException(PackageRefusalReason.Malformed, "The reference is an index, not an artifact.");

            var layers = new List<Layer>();
            if (!root.TryGetProperty("layers", out var array)) return layers;
            foreach (var item in array.EnumerateArray())
            {
                var digest = item.TryGetProperty("digest", out var value) ? value.GetString() ?? string.Empty : string.Empty;
                if (!DigestPattern.IsMatch(digest)) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"'{digest}' is not a layer digest.");

                var media = item.TryGetProperty("mediaType", out var type) ? type.GetString() ?? string.Empty : string.Empty;
                byte[]? signature = null;
                if (item.TryGetProperty("annotations", out var annotations) && annotations.TryGetProperty(PackageSignatureVerifier.SignatureAnnotation, out var encoded))
                {
                    try
                    {
                        signature = Convert.FromBase64String(encoded.GetString() ?? string.Empty);
                    }
                    catch (FormatException)
                    {
                        signature = null;
                    }
                }

                layers.Add(new(digest, media, signature));
            }

            return layers;
        }
        catch (JsonException exception)
        {
            throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The manifest is not valid JSON: {exception.Message}");
        }
    }

    sealed record Layer(string Digest, string MediaType, byte[]? Signature)
    {
        public bool IsArchive => MediaType.Contains("tar", StringComparison.Ordinal);
    }
}
