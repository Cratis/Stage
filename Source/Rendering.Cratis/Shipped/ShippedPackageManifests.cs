// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Packages;

namespace Cratis.Stage.Rendering.Cratis.Shipped;

/// <summary>
/// Reads what the packages the Stage frontend ships declare, from the document generated from their manifests.
/// </summary>
static class ShippedPackageManifests
{
    const string Resource = "Cratis.Stage.Rendering.Cratis.Shipped.ShippedPackages.json";

    /// <summary>
    /// Gets the shipped packages' declarations, in the order the document lists them.
    /// </summary>
    internal static IReadOnlyList<ScenePackage> All { get; } = Load();

    static ScenePackage[] Load()
    {
        using var stream = typeof(ShippedPackageManifests).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException("The shipped package declarations are missing from the renderer.");
        using var document = JsonDocument.Parse(stream);
        return [.. document.RootElement.EnumerateArray().Select(Read)];
    }

    static ScenePackage Read(JsonElement manifest) => new(
        manifest.GetProperty("name").GetString()!,
        manifest.GetProperty("version").GetString()!,
        Enum.Parse<PackageKind>(manifest.GetProperty("kind").GetString()!, ignoreCase: true),
        [.. manifest.GetProperty("dependencies").EnumerateArray().Select(dependency => new PackageDependency(
            dependency.GetProperty("name").GetString()!,
            dependency.TryGetProperty("versionRange", out var range) ? range.GetString() : null))],
        Strings(manifest, "components"),
        Strings(manifest, "layouts"),
        Strings(manifest, "screenTemplates"),
        Strings(manifest, "dialogTemplates"),
        Strings(manifest, "themes"));

    static string[] Strings(JsonElement manifest, string name) => [.. manifest.GetProperty(name).EnumerateArray().Select(value => value.GetString()!)];
}
