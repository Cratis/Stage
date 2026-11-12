// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Serves frontend-only packages from npm. The lock pins the sha256 of the tarball; the tarball is fetched from the registry and refused when its bytes differ.
/// The registry's signature over <c lang="text">name@version:integrity</c> is verified against the publisher's keys, with the integrity computed from the downloaded bytes, so the
/// signature only counts for exactly what was downloaded. The tarball's <c lang="text">package/</c> folder holds the package, with <c lang="text">contract.play</c> at its root.
/// </summary>
/// <param name="http">The <see cref="HttpClient"/>.</param>
/// <param name="options">The <see cref="ExtensionPackageOptions"/>.</param>
public sealed class NpmPackageChannel(HttpClient http, ExtensionPackageOptions options) : IPackageChannel
{
    /// <summary>
    /// The folder in a tarball that holds the package.
    /// </summary>
    public const string Root = "package";

    /// <summary>
    /// The prefix of an npm reference.
    /// </summary>
    public const string Prefix = "npm:";

    const string TarballPart = "tarball.tgz";
    const string MetadataPart = "metadata.json";

    /// <inheritdoc/>
    public bool Handles(string resolved) => resolved.StartsWith(Prefix, StringComparison.Ordinal);

    /// <inheritdoc/>
    public async Task<PackageArtifact> Download(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        var name = NameOf(entry);
        var registry = new Uri(options.NpmRegistry.TrimEnd('/') + "/");
        var metadata = await Get(new(registry, $"{Uri.EscapeDataString(name).Replace("%40", "@")}/{Uri.EscapeDataString(entry.Version)}"), registry, 4 * 1024 * 1024, cancellationToken);

        using var document = ParseMetadata(metadata);
        var tarball = document.RootElement.TryGetProperty("dist", out var dist) && dist.TryGetProperty("tarball", out var address) ? address.GetString() : null;
        if (!Uri.TryCreate(tarball, UriKind.Absolute, out var location) || (location.Scheme != Uri.UriSchemeHttps && !location.IsLoopback)) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"{name}@{entry.Version} names no tarball over a secure connection.");

        var bytes = await Get(location, registry, options.MaxPackageBytes, cancellationToken);
        if (PackageArtifact.Sha256(bytes) != entry.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, $"The tarball of {name}@{entry.Version} has the digest {PackageArtifact.Sha256(bytes)}, not the locked {entry.Digest}.");

        return new(new Dictionary<string, byte[]> { [TarballPart] = bytes, [MetadataPart] = metadata });
    }

    /// <inheritdoc/>
    public FetchedPackage Open(ExtensionLockEntry entry, PackageArtifact artifact)
    {
        if (!artifact.Parts.TryGetValue(TarballPart, out var tarball)) throw new PackageRefusedException(PackageRefusalReason.NotFound, "The package was not downloaded.");
        if (PackageArtifact.Sha256(tarball) != entry.Digest) throw new PackageRefusedException(PackageRefusalReason.DigestMismatch, $"The tarball's digest is {PackageArtifact.Sha256(tarball)}, not the locked {entry.Digest}.");

        var message = Encoding.UTF8.GetBytes($"{NameOf(entry)}@{entry.Version}:{PackageArtifact.Sha512Integrity(tarball)}");
        var signatures = new List<PackageSignature>();
        if (artifact.Parts.TryGetValue(MetadataPart, out var metadata))
        {
            using var document = ParseMetadata(metadata);
            if (document.RootElement.TryGetProperty("dist", out var dist) && dist.TryGetProperty("signatures", out var listed))
            {
                foreach (var item in listed.EnumerateArray())
                {
                    if (item.TryGetProperty("sig", out var sig) && TryDecode(sig.GetString(), out var bytes)) signatures.Add(new(message, bytes));
                }
            }
        }

        return new([tarball], Root, signatures, payload => payload.AsSpan().SequenceEqual(message));
    }

    static string NameOf(ExtensionLockEntry entry)
    {
        var name = entry.Resolved[Prefix.Length..];
        if (name.Length == 0 || name.Contains("..", StringComparison.Ordinal) || name.Any(char.IsWhiteSpace)) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"'{entry.Resolved}' is not an npm package.");
        return name;
    }

    static JsonDocument ParseMetadata(byte[] metadata)
    {
        try
        {
            return JsonDocument.Parse(metadata);
        }
        catch (JsonException exception)
        {
            throw new PackageRefusedException(PackageRefusalReason.Malformed, $"The registry's description of the package is not valid JSON: {exception.Message}");
        }
    }

    static bool TryDecode(string? text, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(text ?? string.Empty);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    async Task<byte[]> Get(Uri address, Uri registry, long maxBytes, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        if (address.Host == registry.Host && options.Registries.TryGetValue(registry.Host, out var credentials))
        {
            request.Headers.Authorization = credentials.Username.Length == 0
                ? new AuthenticationHeaderValue("Bearer", credentials.Password)
                : new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}")));
        }

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The npm registry answered {(int)response.StatusCode} for {address}.");

            return await BoundedResponse.Read(response, maxBytes, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The npm registry could not be reached: {exception.Message}");
        }
    }
}
