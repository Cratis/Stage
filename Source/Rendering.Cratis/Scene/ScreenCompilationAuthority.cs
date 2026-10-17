// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Retains the source authority missing from the executable model's screen-free contract.
/// </summary>
internal static class ScreenCompilationAuthority
{
    internal static Source Capture(SemanticCompilation compilation) => new(
        compilation.Model.Application.Name,
        Encoding.UTF8.GetString(SemanticIdentityCatalogSerializer.Serialize(compilation.Documents.IdentityCatalog)),
        [.. compilation.Documents.Documents.Select(document => new Document(document.StableKey, document.DisplayPath, document.Text))],
        new SortedDictionary<string, string>(compilation.Documents.AttachmentContents, StringComparer.Ordinal));

    internal static ScreenplayWorkspace Open(Source source, string revision)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(source.IdentityCatalog));
        var workspace = ScreenplayWorkspace.Create(
            catalog.Application,
            source.ApplicationName,
            [.. source.Documents.Select(document => WorkspaceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, PortablePlayPath.Parse(document.Path), Encoding.UTF8.GetBytes(document.Text)))],
            catalog,
            source.Attachments.ToImmutableDictionary(StringComparer.Ordinal),
            []);
        if (workspace.Compilation.Value?.Model.Revision.ToString() != revision)
        {
            throw new InvalidArtifactRenderContract("Screen partitions require source authority from the same compiled model revision.");
        }

        return workspace;
    }

    internal static List<Declaration> Declarations(ScreenplayWorkspace workspace)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Diagnostics.Any(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error))
        {
            throw new InvalidArtifactRenderContract("Screen partitions contain invalid source authority.");
        }

        var paths = workspace.Compilation.Value!.Model.Application.Modules.SelectMany(module => module.Features.SelectMany(feature => SlicePaths(feature, [module.Name])))
            .ToDictionary(located => located.Slice.Id, located => located.Path);

        return [.. index.Entries.Where(entry => entry.Node is SliceSyntax && entry.SemanticId is not null)
            .SelectMany(entry => ((SliceSyntax)entry.Node).Screens.Select(screen => new Declaration(entry.SemanticId!.Value.ToString(), Key(paths[entry.SemanticId.Value], screen.Name), screen.Contributions?.Count() ?? 0)))];
    }

    internal static JsonArray Screens(ScreenplayWorkspace workspace)
    {
        var texts = workspace.Documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var compiled = new PlayFileCompiler(new SourceFiles(texts), new ScreenplayCompiler()).CompileFolder("screen-authority").Result;
        if (!compiled.Success)
        {
            throw new InvalidArtifactRenderContract("Screen partitions cannot compile their retained Scene source authority.");
        }

        var translated = new ScreenplaySceneVisitor().Visit(compiled.Value!);
        var errors = translated.ConversionDiagnostics.Where(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidArtifactRenderContract(string.Join(Environment.NewLine, errors.Select(error => $"{error.Code}: {error.Message}")));
        }

        return JsonNode.Parse(CanonicalSceneJson.Serialize(translated))!["screens"]!.AsArray();
    }

    internal static string Key(JsonObject screen) => CanonicalSceneJson.Write(new JsonArray(screen["path"]?.DeepClone(), screen["name"]?.DeepClone()));

    static string Key(string[] path, string name) => CanonicalSceneJson.Write(new JsonArray(new JsonArray([.. path.Select(segment => JsonValue.Create(segment))]), JsonValue.Create(name)));

    static IEnumerable<(SemanticSlice Slice, string[] Path)> SlicePaths(SemanticFeature feature, string[] parent)
    {
        string[] path = [.. parent, feature.Name];

        return feature.Slices.Select(slice => (slice, path)).Concat(feature.Features.SelectMany(child => SlicePaths(child, path)));
    }

    internal sealed record Source(string ApplicationName, string IdentityCatalog, Document[] Documents, SortedDictionary<string, string> Attachments);
    internal sealed record Document(string StableKey, string Path, string Text);
    internal sealed record Declaration(string Owner, string Key, int OwnContributions);

    sealed class SourceFiles(IReadOnlyDictionary<string, string> texts) : IPlayFiles
    {
        public IEnumerable<PlayFile> FindIn(string root)
        {
            var folder = root.Replace('\\', '/')["screen-authority".Length..].TrimStart('/');
            var prefix = folder.Length == 0 ? string.Empty : $"{folder}/";

            return texts.Keys.Where(path => path.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)
                .Select(path => new PlayFile(path, path[prefix.Length..]));
        }

        public string ReadContent(PlayFile file) => texts.TryGetValue(file.Path, out var text)
            ? text
            : throw new InvalidArtifactRenderContract("Screen partitions reference a document outside their retained source authority.");
    }
}
