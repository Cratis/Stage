// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Xml;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Carries syntax-only documentation through hashed profile inputs without changing the executable model.
/// </summary>
internal static class AuthoringMetadataInput
{
    internal const string Name = "cratis-authoring:documentation";
    const string Version = "1";

    internal static ArtifactRenderInput? Create(SemanticCompilation compilation)
    {
        var documents = compilation.Documents;
        var workspace = ScreenplayWorkspace.Create(
            documents.IdentityCatalog.Application,
            compilation.Model.Application.Name,
            [.. documents.Documents.Select(document => WorkspaceDocument.Create(
                document.Id,
                document.StableKey,
                PortablePlayPath.Parse(document.DisplayPath),
                Encoding.UTF8.GetBytes(document.Text)))],
            documents.IdentityCatalog);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Diagnostics.Any(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error) ||
            workspace.Compilation.Value?.Model.Revision != compilation.Model.Revision)
        {
            throw new InvalidArtifactRenderContract("Authoring metadata requires the syntax and identity catalog from the same semantic compilation.");
        }

        var ids = DeclarationIds(compilation.Model).ToHashSet();
        var eventRevisions = compilation.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices)
            .SelectMany(slice => slice.Events).ToDictionary(@event => @event.Id, @event => @event.Revision.Value);
        var entries = new SortedDictionary<string, Metadata>(StringComparer.Ordinal);

        // All syntax generations share a semantic identity; only the ESM's current revision owns its metadata.
        foreach (var entry in index.Entries.Where(entry => entry.SemanticId is not null && ids.Contains(entry.SemanticId.Value) &&
            (entry.Node is not EventSyntax @event || @event.Generation == eventRevisions[entry.SemanticId.Value])))
        {
            Metadata metadata;
            try
            {
                metadata = Of(entry.Node);
            }
            catch (XmlException exception)
            {
                throw new InvalidArtifactRenderContract($"Authoring metadata for declaration '{entry.SemanticId}' contains invalid XML characters: {exception.Message}");
            }

            if (metadata.Description is null && metadata.Documentation is null)
            {
                continue;
            }

            var key = entry.SemanticId!.Value.ToString();
            if (!entries.TryAdd(key, metadata) && entries[key] != metadata)
            {
                throw new InvalidArtifactRenderContract($"Conflicting authoring metadata for declaration '{key}'.");
            }
        }

        return entries.Count == 0 ? null : ArtifactRenderInput.Create(
            Name,
            Version,
            [.. JsonSerializer.SerializeToUtf8Bytes(new Catalog(compilation.Model.Revision.ToString(), entries))]);
    }

    internal static Metadata Of(SyntaxNode? node) => node switch
    {
        EventSyntax declaration => new(Normalize(declaration.Description), Normalize(declaration.Documentation)),
        CommandSyntax declaration => new(Normalize(declaration.Description), null),
        ReadModelSyntax declaration => new(Normalize(declaration.Description), null),
        QuerySyntax declaration => new(Normalize(declaration.Description), null),
        TypeSyntax declaration => new(Normalize(declaration.Description), null),
        ReactionSyntax declaration => new(Normalize(declaration.Description), null),
        ReactionTriggerSyntax declaration => new(Normalize(declaration.Description), null),
        _ => new(null, null)
    };

    internal static bool TryRead(ArtifactRenderInput input, ExecutableSemanticModel model, out Catalog? catalog)
    {
        catalog = null;
        if (input.Name != Name || input.Version != Version)
        {
            return false;
        }

        try
        {
            var candidate = JsonSerializer.Deserialize<Catalog>(input.Bytes.AsSpan());
            var ids = DeclarationIds(model).Select(id => id.ToString()).ToHashSet(StringComparer.Ordinal);
            if (candidate?.Entries is null || candidate.ModelRevision != model.Revision.ToString() || candidate.Entries.Count == 0 ||
                candidate.Entries.Any(entry => !ids.Contains(entry.Key) || entry.Value is null ||
                    (entry.Value.Description is null && entry.Value.Documentation is null) ||
                    entry.Value.Description != Normalize(entry.Value.Description) || entry.Value.Documentation != Normalize(entry.Value.Documentation)))
            {
                return false;
            }

            var canonical = new Catalog(candidate.ModelRevision, new SortedDictionary<string, Metadata>(candidate.Entries, StringComparer.Ordinal));
            if (!input.Bytes.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(canonical)))
            {
                return false;
            }

            catalog = canonical;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or XmlException)
        {
            return false;
        }
    }

    static IEnumerable<SemanticId> DeclarationIds(ExecutableSemanticModel model) =>
        model.Application.Concepts.Select(concept => concept.Id).Concat(model.Application.Types.Select(type => type.Id))
            .Concat(model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices)
                .SelectMany(slice => slice.Events.Select(declaration => declaration.Id)
                    .Concat(slice.Commands.Select(declaration => declaration.Id))
                    .Concat(slice.ReadModels.Select(declaration => declaration.Id))
                    .Concat(slice.Queries.Select(declaration => declaration.Id))));

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static string? Normalize(string? text) => text is null ? null : XmlConvert.VerifyXmlChars(CSharpCodeBuilder.NormalizeDocumentation(text));

    internal sealed record Metadata(string? Description, string? Documentation)
    {
        internal CSharpCodeBuilder Render(CSharpCodeBuilder builder, string? fallbackSummary = null) =>
            builder.Documentation(Description, Documentation, fallbackSummary);
    }

    internal sealed record Catalog(string ModelRevision, SortedDictionary<string, Metadata> Entries);
}
