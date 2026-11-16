// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Carries canonical binary assets in the render profile, outside portable semantic bytes.
/// </summary>
internal static class ModelAssetsInput
{
    internal const string Name = "cratis-realization:model-assets";
    internal const string Version = "1";
    const int MaximumFileBytes = 5 * 1024 * 1024;
    const int MaximumTotalBytes = 25 * 1024 * 1024;

    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, ModelAssets assets)
    {
        var inputs = profile.Inputs.Where(input => input.Name != Name).ToImmutableArray();
        if (assets.Files.IsEmpty && inputs.Length == profile.Inputs.Length) return profile;
        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, assets.Files.IsEmpty ? inputs : inputs.Add(ArtifactRenderInput.Create(Name, Version, [.. Serialize(assets)])));
    }

    internal static bool TryRead(ArtifactRenderProfile profile, out ModelAssets? assets)
    {
        assets = null;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        return inputs.Length == 1 && TryRead(inputs[0], out assets);
    }

    internal static ModelAssets? From(ArtifactRenderProfile profile) => TryRead(profile, out var assets) ? assets : null;

    static bool TryRead(ArtifactRenderInput input, out ModelAssets? assets)
    {
        assets = null;
        if (input.Version != Version) return false;
        if (input.Sha256 != Convert.ToHexStringLower(SHA256.HashData(input.Bytes.AsSpan()))) return false;
        try
        {
            var wire = JsonSerializer.Deserialize<Dictionary<string, string>>(new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan()));
            if (wire is null || !TryDecode(wire, out var read)) return false;
            if (!Serialize(read).SequenceEqual(input.Bytes)) return false;
            assets = read;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    static bool TryDecode(Dictionary<string, string> wire, out ModelAssets assets)
    {
        assets = ModelAssets.None;
        var files = assets.Files.ToBuilder();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var (path, encoded) in wire)
        {
            if (!IsAdmittedPath(path, names)) return false;
            if (encoded is null || encoded.Length > ((MaximumFileBytes + 2) / 3 * 4)) return false;
            var bytes = Convert.FromBase64String(encoded);
            total += bytes.Length;
            if (bytes.Length > MaximumFileBytes || total > MaximumTotalBytes) return false;
            files.Add(path, [.. bytes]);
        }
        assets = new(files.ToImmutable(), []);
        return true;
    }

    static bool IsAdmittedPath(string path, HashSet<string> names) =>
        ModelAssets.IsSafePath(path) && !ModelAssets.CollidesWithBundle(path) && names.Add(path);

    static byte[] Serialize(ModelAssets assets) => JsonSerializer.SerializeToUtf8Bytes(
        new SortedDictionary<string, string>(assets.Files.ToDictionary(file => file.Key, file => Convert.ToBase64String(file.Value.AsSpan())), StringComparer.Ordinal));
}
