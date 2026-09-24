// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The compiled executable model and its capability-admitted plan.
/// </summary>
/// <param name="Model">The executable semantic model.</param>
/// <param name="Plan">The execution plan.</param>
public sealed record LoadedSemanticModel(ExecutableSemanticModel Model, SemanticExecutionPlan Plan);

/// <summary>
/// Loads portable Screenplay semantic execution from one file or a folder of source documents.
/// </summary>
public static class SemanticModelLoader
{
    /// <summary>
    /// Compiles a Screenplay source set and optional authoritative identity catalog.
    /// </summary>
    /// <param name="path">A .play file or a folder searched recursively for .play sources.</param>
    /// <param name="catalogPath">An optional canonical Screenplay identity catalog JSON file.</param>
    /// <returns>The executable model and plan.</returns>
    /// <exception cref="InvalidSemanticModel">Input is absent, malformed, or not executable.</exception>
    public static async Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath = null)
    {
        var isFolder = Directory.Exists(path);
        if (!isFolder && (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidSemanticModel(["Supply an existing .play file or a directory containing .play files."]);
        }

        var files = isFolder ? Directory.GetFiles(path, "*.play", SearchOption.AllDirectories) : [path];
        if (files.Length == 0)
        {
            throw new InvalidSemanticModel(["No .play files were found."]);
        }

        var root = isFolder ? path : Path.GetDirectoryName(Path.GetFullPath(path))!;
        var name = isFolder ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);
        var catalog = catalogPath is null
            ? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name))
            : SemanticIdentityCatalogSerializer.Deserialize(await File.ReadAllBytesAsync(catalogPath));
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            // The catalog uses opaque, non-path keys. Encode UTF-8 bytes so even a directory separator cannot
            // be mistaken for a path component while retaining deterministic identity across host platforms.
            var key = Convert.ToHexString(Encoding.UTF8.GetBytes(relative));
            documents.Add(SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, relative, await File.ReadAllTextAsync(file)));
        }

        var compiled = new SemanticModelCompiler().Compile(name, SemanticDocumentSet.Create([.. documents], catalog));
        if (!compiled.Success)
        {
            throw new InvalidSemanticModel([.. compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}")]);
        }

        var model = compiled.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model);
        if (!plan.Success)
        {
            throw new InvalidSemanticModel([.. plan.Issues.Select(issue => $"{issue.Artifact}: {issue.Details}")]);
        }

        return new(model, plan.Plan!);
    }
}
