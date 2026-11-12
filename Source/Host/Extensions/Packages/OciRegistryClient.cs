// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// The place of a package in an OCI registry.
/// </summary>
/// <param name="Host">The registry host, with the port when there is one.</param>
/// <param name="Repository">The repository.</param>
public sealed partial record OciReference(string Host, string Repository)
{
    /// <summary>
    /// Gets the base address of the registry. Only a loopback registry may be reached without TLS.
    /// </summary>
    public Uri BaseAddress => new($"{(IsLoopback ? "http" : "https")}://{Host}/");

    [GeneratedRegex(@"^(?<host>(?:[a-z0-9.-]+|\[::1\])(?::[0-9]{1,5})?)/(?<repository>[a-z0-9]+(?:[._-][a-z0-9]+)*(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)*)$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Reference { get; }

    bool IsLoopback => Array.Exists(["localhost", "127.0.0.1", "[::1]"], name => string.Equals(Host.Split(':')[0], name, StringComparison.Ordinal));

    /// <summary>
    /// Reads a resolved reference such as <c lang="text">ghcr.io/cratis/extension-mail</c>.
    /// </summary>
    /// <param name="resolved">The resolved reference of a lock entry.</param>
    /// <returns>The <see cref="OciReference"/>.</returns>
    /// <exception cref="PackageRefusedException">Thrown when it is not a registry reference.</exception>
    public static OciReference Parse(string resolved)
    {
        var match = Reference.Match(resolved);
        if (!match.Success) throw new PackageRefusedException(PackageRefusalReason.Malformed, $"'{resolved}' is not a registry reference without a tag or digest.");
        return new(match.Groups["host"].Value, match.Groups["repository"].Value);
    }
}

/// <summary>
/// Talks to an OCI registry to read manifests and blobs, authenticating with the credentials the deployment configures and no others.
/// </summary>
/// <param name="http">The <see cref="HttpClient"/>.</param>
/// <param name="options">The <see cref="ExtensionPackageOptions"/>.</param>
public sealed partial class OciRegistryClient(HttpClient http, ExtensionPackageOptions options)
{
    [GeneratedRegex("(?<name>\\w+)=\"(?<value>[^\"]*)\"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HeaderParameter { get; }

    /// <summary>
    /// Reads a manifest or a blob.
    /// </summary>
    /// <param name="reference">The <see cref="OciReference"/>.</param>
    /// <param name="kind">Either <c lang="text">manifests</c> or <c lang="text">blobs</c>.</param>
    /// <param name="identifier">The digest, or for a manifest a tag.</param>
    /// <param name="maxBytes">The most bytes to accept.</param>
    /// <param name="cancellationToken">Cancelled when loading stops.</param>
    /// <returns>The bytes, or null when the registry does not have it.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the registry refuses or the answer is too large.</exception>
    public async Task<byte[]?> Get(OciReference reference, string kind, string identifier, long maxBytes, CancellationToken cancellationToken)
    {
        var address = new Uri(reference.BaseAddress, $"v2/{reference.Repository}/{kind}/{identifier}");
        using var first = await Send(address, reference, kind, null, cancellationToken);
        if (first.StatusCode != HttpStatusCode.Unauthorized) return await Read(first, maxBytes, cancellationToken);

        var authorization = await Authorize(first, reference, cancellationToken)
            ?? throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The registry {reference.Host} requires credentials for {reference.Repository}; configure them under {ExtensionPackageOptions.Section}:Registries.");
        using var second = await Send(address, reference, kind, authorization, cancellationToken);
        return await Read(second, maxBytes, cancellationToken);
    }

    static async Task<byte[]?> Read(HttpResponseMessage response, long maxBytes, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The registry answered {(int)response.StatusCode} for {response.RequestMessage?.RequestUri}.");

        return await BoundedResponse.Read(response, maxBytes, cancellationToken);
    }

    static Dictionary<string, string> Parameters(string text) => HeaderParameter.Matches(text).ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value, StringComparer.OrdinalIgnoreCase);

    async Task<HttpResponseMessage> Send(Uri address, OciReference reference, string kind, AuthenticationHeaderValue? authorization, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, address) { Headers = { Authorization = authorization } };
        if (kind == "manifests")
        {
            request.Headers.Accept.ParseAdd("application/vnd.oci.image.manifest.v1+json");
            request.Headers.Accept.ParseAdd("application/vnd.docker.distribution.manifest.v2+json");
        }

        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new PackageRefusedException(PackageRefusalReason.NotFound, $"The registry {reference.Host} could not be reached: {exception.Message}");
        }
    }

    async Task<AuthenticationHeaderValue?> Authorize(HttpResponseMessage challenge, OciReference reference, CancellationToken cancellationToken)
    {
        options.Registries.TryGetValue(reference.Host, out var credentials);
        var header = challenge.Headers.WwwAuthenticate.FirstOrDefault();
        if (header is null) return null;

        if (header.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            return credentials is null ? null : new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}")));
        }

        var parameters = Parameters(header.Parameter ?? string.Empty);
        if (!header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) || !parameters.TryGetValue("realm", out var realm) || !Uri.TryCreate(realm, UriKind.Absolute, out var tokenAddress)) return null;
        if (tokenAddress.Scheme != Uri.UriSchemeHttps && !tokenAddress.IsLoopback) throw new PackageRefusedException(PackageRefusalReason.NotFound, "The registry asked for a token over an insecure connection.");

        var query = $"service={Uri.EscapeDataString(parameters.GetValueOrDefault("service", string.Empty))}&scope={Uri.EscapeDataString(parameters.GetValueOrDefault("scope", $"repository:{reference.Repository}:pull"))}";
        using var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder(tokenAddress) { Query = query }.Uri);
        if (credentials is not null) request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}")));
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        string? token = null;
        if (document.RootElement.TryGetProperty("token", out var value)) token = value.GetString();
        else if (document.RootElement.TryGetProperty("access_token", out var access)) token = access.GetString();

        return token is null ? null : new("Bearer", token);
    }
}
