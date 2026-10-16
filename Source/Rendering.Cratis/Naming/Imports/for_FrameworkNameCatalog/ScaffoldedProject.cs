// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Represents what MSBuild resolved for one restored scaffold project: its compile references, its global usings, the
/// source files packages add to its compilation and the packages in its restore graph.
/// </summary>
/// <param name="Project">Which generated project this is.</param>
/// <param name="Directory">The project directory.</param>
/// <param name="References">The compile references.</param>
/// <param name="Usings">The <c language="csharp">Using</c> items.</param>
/// <param name="PackageSources">The compile items packages contribute from outside the project directory.</param>
/// <param name="GeneratedSources">The files source generators added to the compilation.</param>
/// <param name="Packages">The packages in the restore graph, with their resolved versions.</param>
sealed record ScaffoldedProject(
    GeneratedProjects Project,
    string Directory,
    IReadOnlyList<ScaffoldedReference> References,
    IReadOnlyList<CatalogUsing> Usings,
    IReadOnlyList<(string Package, string Path)> PackageSources,
    IReadOnlyList<string> GeneratedSources,
    IReadOnlyList<(string Id, string Version)> Packages)
{
    internal IReadOnlyList<string> NamespaceReferences { get; init; } = [];
    internal IReadOnlyList<string> ProjectSources { get; init; } = [];

    /// <summary>
    /// Reads a project from the JSON MSBuild prints for <c language="csharp">-getItem</c> and the restore assets file.
    /// </summary>
    /// <param name="project">Which generated project this is.</param>
    /// <param name="directory">The project directory.</param>
    /// <param name="items">The MSBuild items JSON.</param>
    /// <param name="generatedSources">The directory the compiler wrote generated sources to.</param>
    /// <returns>The project.</returns>
    public static ScaffoldedProject Read(GeneratedProjects project, string directory, string items, string generatedSources)
    {
        using var document = JsonDocument.Parse(items);
        var root = document.RootElement.GetProperty("Items");
        var references = root.GetProperty("ReferencePathWithRefAssemblies").EnumerateArray()
            .Where(item => Metadata(item, "ReferenceSourceTarget") != "ProjectReference")
            .Select(item => new ScaffoldedReference(
                Metadata(item, "FullPath"),
                Metadata(item, "NuGetPackageId"),
                Metadata(item, "FrameworkReferenceName")))
            .ToArray();
        var usings = root.GetProperty("Using").EnumerateArray().Select(item => new CatalogUsing(
            project,
            UsingKind(item),
            Metadata(item, "Identity"),
            Metadata(item, "Alias"),
            Metadata(item, "DefiningProjectName"))).ToArray();
        var packageSources = root.GetProperty("Compile").EnumerateArray()
            .Select(item => Path.GetFullPath(Metadata(item, "FullPath")))
            .Where(path => !path.StartsWith(Path.GetFullPath(directory), StringComparison.Ordinal))
            .Select(path => (Package: PackageOf(path), Path: path))
            .ToArray();
        var generated = System.IO.Directory.Exists(generatedSources)
            ? System.IO.Directory.GetFiles(generatedSources, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray()
            : [];

        var assets = Path.Combine(directory, "obj", "project.assets.json");
        return new(project, directory, references, usings, packageSources, generated, Assets(assets))
        {
            NamespaceReferences = ScaffoldedNamespaces.PackageAssemblies(assets),
            ProjectSources = [.. root.GetProperty("Compile").EnumerateArray()
                .Select(item => Path.GetFullPath(Metadata(item, "FullPath")))
                .Except(packageSources.Select(source => source.Path), StringComparer.Ordinal)]
        };
    }

    /// <summary>
    /// Gets the id of the package a file belongs to, from its path in the global packages folder.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The package id as the packages folder spells it, lower case.</returns>
    public static string PackageOf(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar);
        var index = Array.FindLastIndex(segments, segment => segment == "packages");
        return index >= 0 && index + 1 < segments.Length ? segments[index + 1] : Path.GetFileName(path);
    }

    static IReadOnlyList<(string Id, string Version)> Assets(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return
        [
            .. document.RootElement.GetProperty("libraries").EnumerateObject()
                .Where(library => library.Value.GetProperty("type").GetString() == "package")
                .Select(library => library.Name.Split('/'))
                .Select(parts => (parts[0], parts[1]))
        ];
    }

    static CatalogUsingKind UsingKind(JsonElement item) => (Metadata(item, "Static"), Metadata(item, "Alias")) switch
    {
        ("true", _) => CatalogUsingKind.Static,
        (_, { Length: > 0 }) => CatalogUsingKind.Alias,
        _ => CatalogUsingKind.Namespace
    };

    static string Metadata(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
