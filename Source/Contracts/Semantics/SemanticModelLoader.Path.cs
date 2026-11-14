// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics;

public static partial class SemanticModelLoader
{
    internal static async Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath, string? applicationName, bool allowRefusedSpecifications)
    {
        var isFolder = Directory.Exists(path);
        var files = SourceFiles(path, isFolder);
        var root = isFolder ? path : Path.GetDirectoryName(Path.GetFullPath(path))!;
        var name = applicationName ?? (isFolder ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path));
        PersistedIdentityState? state = null;
        var catalog = catalogPath is null ? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name)) :
            DeserializeCatalog(await File.ReadAllBytesAsync(catalogPath), out state);
        if (state is not null && IsRootIdentityPath(Path.GetFullPath(root), Path.GetFullPath(catalogPath!)))
        {
            var opened = LoadModelRoot(Path.GetFullPath(root), [.. files.Select(Path.GetFullPath)], isFolder ? [] : [Path.GetFullPath(path)], applicationName ?? state.ApplicationName, default, allowRefusedSpecifications);
            if (!opened.Success) throw new InvalidSemanticModel([.. opened.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)]) { Notes = opened.Notes };
            return opened.Loaded!;
        }
        var documents = await ReadDocuments(root, files, state, catalog);
        return RequireCompilation(Compile(root, name, catalog, documents, default, allowRefusedSpecifications));
    }

    static string[] SourceFiles(string path, bool isFolder)
    {
        if (!isFolder && (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidSemanticModel(["Supply an existing .play file or a directory containing .play files."]);
        }
        var files = isFolder ? Directory.GetFiles(path, "*.play", SearchOption.AllDirectories) : [path];
        if (files.Length == 0) throw new InvalidSemanticModel(["No .play files were found."]);
        return files;
    }

    static async Task<ImmutableArray<SemanticSourceDocument>> ReadDocuments(string root, string[] files, PersistedIdentityState? state, SemanticIdentityCatalog catalog)
    {
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var mapping = state?.Documents.SingleOrDefault(document => document.Path.Value == relative);
            var key = mapping?.StableKey ?? Convert.ToHexString(Encoding.UTF8.GetBytes(relative));
            documents.Add(SemanticSourceDocument.Create(mapping?.Id ?? catalog.ResolveDocument(key), key, relative, await File.ReadAllTextAsync(file)));
        }
        return [.. documents];
    }

    static LoadedSemanticModel RequireCompilation(SemanticModelLoadResult result)
    {
        if (!result.Success)
        {
            throw new InvalidSemanticModel([.. result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code != "STAGE-PLAN-003")
                .Select(diagnostic => diagnostic.Source is null ? diagnostic.Message : $"{diagnostic.Source}: {diagnostic.Message}")])
            { Notes = result.Notes };
        }
        return result.Loaded!;
    }
}
