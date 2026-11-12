// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages.given;

/// <summary>
/// An OCI registry and an npm registry in memory, answering by path and recording the requests made.
/// </summary>
public sealed class FakeRegistry : HttpMessageHandler
{
    public const string LayerType = "application/vnd.cratis.extension.layer.v1.tar+gzip";

    readonly Dictionary<string, byte[]> _answers = [];

    public List<string> Requests { get; } = [];

    public List<string?> Authorizations { get; } = [];

    public bool RequireToken { get; set; }

    public static string Digest(byte[] bytes) => ExtensionHashes.Of(bytes);

    public void Put(string path, byte[] bytes) => _answers[path] = bytes;

    public byte[] Publish(string repository, byte[] archive, byte[]? payload = null, byte[]? signature = null)
    {
        var layer = Digest(archive);
        Put($"/v2/{repository}/blobs/{layer}", archive);
        var manifest = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { schemaVersion = 2, mediaType = "application/vnd.oci.image.manifest.v1+json", layers = new[] { new { mediaType = LayerType, digest = layer, size = archive.Length } } }));
        var digest = Digest(manifest);
        Put($"/v2/{repository}/manifests/{digest}", manifest);
        if (payload is not null && signature is not null)
        {
            var payloadDigest = Digest(payload);
            Put($"/v2/{repository}/blobs/{payloadDigest}", payload);
            var signed = new { schemaVersion = 2, layers = new[] { new { mediaType = "application/vnd.dev.cosign.simplesigning.v1+json", digest = payloadDigest, size = payload.Length, annotations = new Dictionary<string, string> { [PackageSignatureVerifier.SignatureAnnotation] = Convert.ToBase64String(signature) } } } };
            Put($"/v2/{repository}/manifests/sha256-{digest["sha256:".Length..]}.sig", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(signed)));
        }

        return manifest;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        Requests.Add(path);
        Authorizations.Add(request.Headers.Authorization?.ToString());

        if (path == "/token")
        {
            var token = request.Headers.Authorization is null ? null : "granted";
            return Task.FromResult(token is null ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"token\":\"granted\"}") });
        }

        if (RequireToken && request.Headers.Authorization?.Scheme != "Bearer")
        {
            var challenge = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            challenge.Headers.WwwAuthenticate.ParseAdd($"Bearer realm=\"http://localhost{(request.RequestUri.Port > 0 ? ":" + request.RequestUri.Port : string.Empty)}/token\",service=\"registry\",scope=\"repository:acme/extension-mail:pull\"");
            return Task.FromResult(challenge);
        }

        return Task.FromResult(_answers.TryGetValue(path, out var bytes) ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) } : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
