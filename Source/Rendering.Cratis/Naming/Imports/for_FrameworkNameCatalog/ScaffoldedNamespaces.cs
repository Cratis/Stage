// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Retains package namespaces even when a newer SDK substitutes a framework reference assembly for a package library.
/// Only namespace existence is unioned; the compiler's selected references still define the catalog's visible types.
/// </summary>
static class ScaffoldedNamespaces
{
    internal static IReadOnlyList<string> PackageAssemblies(string assetsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var root = document.RootElement;
        var folders = root.GetProperty("packageFolders").EnumerateObject().Select(folder => folder.Name).ToArray();
        var libraries = root.GetProperty("libraries");
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in root.GetProperty("targets").EnumerateObject())
        {
            foreach (var library in target.Value.EnumerateObject())
            {
                var metadata = libraries.GetProperty(library.Name);
                if (metadata.GetProperty("type").GetString() != "package") continue;
                var package = metadata.GetProperty("path").GetString()!;
                foreach (var group in new[] { "compile", "runtime" })
                {
                    if (!library.Value.TryGetProperty(group, out var assemblies)) continue;
                    foreach (var assembly in assemblies.EnumerateObject().Where(assembly => assembly.Name.EndsWith(".dll", StringComparison.Ordinal)))
                    {
                        var path = folders.Select(folder => Path.Combine(folder, package, assembly.Name)).FirstOrDefault(File.Exists)
                            ?? throw new CatalogScaffoldFailed("read package namespace metadata", $"Missing {package}/{assembly.Name}");
                        paths.Add(path);
                    }
                }
            }
        }

        return [.. paths.Order(StringComparer.Ordinal)];
    }
}
