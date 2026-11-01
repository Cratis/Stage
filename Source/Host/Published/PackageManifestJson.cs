// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Reads and writes manifests. Writing is byte-for-byte reproducible: declared property order, LF line endings, no timestamps.
/// </summary>
internal static class PackageManifestJson
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NewLine = "\n"
    };

    internal static string Write(PackageManifest manifest) => JsonSerializer.Serialize(manifest, _options) + "\n";

    internal static PackageManifest Read(string json) =>
        JsonSerializer.Deserialize<PackageManifest>(json, _options) ?? throw new PublishedModeRefused(["The package manifest is empty."]);
}
