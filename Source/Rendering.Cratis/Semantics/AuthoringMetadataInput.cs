// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Xml;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
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
        var index = SyntaxFor(compilation);
        var entries = MetadataEntries(index, compilation);

        return entries.Count == 0 ? null : ArtifactRenderInput.Create(
            Name,
            Version,
            [.. JsonSerializer.SerializeToUtf8Bytes(new Catalog(compilation.Model.Revision.ToString(), entries))]);
    }

    internal static Metadata Of(SyntaxNode? node) => node switch
    {
        EventSyntax declaration => new(Normalize(declaration.Description), Normalize(declaration.Documentation)),
        CommandSyntax declaration => new(Normalize(declaration.Description), Normalize(declaration.Documentation)),
        ReadModelSyntax declaration => new(Normalize(declaration.Description), Normalize(declaration.Documentation)),
        ReactionSyntax declaration => new(Normalize(declaration.Description), Normalize(declaration.Documentation)),
        _ => new(DescriptionOf(node), null)
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
            if (!IsForModel(candidate, model))
            {
                return false;
            }

            var canonical = new Catalog(candidate!.ModelRevision, new SortedDictionary<string, Metadata>(candidate.Entries, StringComparer.Ordinal));
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

    static WorkspaceSyntaxIndex SyntaxFor(SemanticCompilation compilation)
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
            documents.IdentityCatalog,

            // File-included bodies (handler file ...) bind from the attachment contents; without them the
            // rebuilt model differs from the compilation being rendered.
            documents.AttachmentContents,
            []);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Diagnostics.Any(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error) ||
            !MatchesModel(workspace, compilation))
        {
            throw new InvalidArtifactRenderContract("Authoring metadata requires the syntax and identity catalog from the same semantic compilation.");
        }

        return index;
    }

    static bool MatchesModel(ScreenplayWorkspace workspace, SemanticCompilation compilation)
    {
        if (workspace.Compilation.Success)
        {
            return workspace.Compilation.Value!.Model.Revision == compilation.Model.Revision;
        }

        // Screenplay admits only source-valid documents; attributable binder failures may exclude units.
        // Recompile the same documents and catalog, never accept an arbitrary subset of their declarations.
        var admissible = new SemanticModelCompiler().CompileAdmissible(compilation.Model.Application.Name, compilation.Documents);
        return admissible.Compilation.Success && !admissible.Exclusions.IsEmpty &&
            admissible.Compilation.Value!.Model.Revision == compilation.Model.Revision;
    }

    static SortedDictionary<string, Metadata> MetadataEntries(WorkspaceSyntaxIndex index, SemanticCompilation compilation)
    {
        var model = compilation.Model;
        var ids = DeclarationIds(model).ToHashSet();
        var eventRevisions = model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices)
            .SelectMany(slice => slice.Events).ToDictionary(@event => @event.Id, @event => @event.Revision.Value);
        var entries = new SortedDictionary<string, Metadata>(StringComparer.Ordinal);

        // All syntax generations share a semantic identity; only the ESM's current revision owns its metadata.
        // A failed ordinary workspace compile does not materialize declaration assignments. Resolve
        // supported addresses through the authoritative catalog, then restrict them to the surviving model.
        var declarations = index.Entries.Where(entry => entry.Address is not null)
            .Select(entry => entry with { SemanticId = entry.SemanticId ?? compilation.Documents.IdentityCatalog.ResolveSemantic(entry.Address!) });
        foreach (var entry in declarations.Where(entry => OwnsMetadata(entry, ids, eventRevisions)))
        {
            var metadata = MetadataFor(entry);
            if (IsEmpty(metadata))
            {
                continue;
            }

            var key = entry.SemanticId!.Value.ToString();
            if (!entries.TryAdd(key, metadata) && entries[key] != metadata)
            {
                throw new InvalidArtifactRenderContract($"Conflicting authoring metadata for declaration '{key}'.");
            }
        }

        return entries;
    }

    static bool OwnsMetadata(WorkspaceSyntaxEntry entry, HashSet<SemanticId> ids, Dictionary<SemanticId, uint> eventRevisions) =>
        entry.SemanticId is { } id && ids.Contains(id) &&
        (entry.Node is not EventSyntax @event || @event.Generation == eventRevisions[id]);

    static Metadata MetadataFor(WorkspaceSyntaxEntry entry)
    {
        try
        {
            return Of(entry.Node);
        }
        catch (XmlException exception)
        {
            throw new InvalidArtifactRenderContract($"Authoring metadata for declaration '{entry.SemanticId}' contains invalid XML characters: {exception.Message}");
        }
    }

    static string? DescriptionOf(SyntaxNode? node) => Normalize(node switch
    {
        QuerySyntax declaration => declaration.Description,
        TypeSyntax declaration => declaration.Description,
        SpecificationSyntax declaration => declaration.Description,
        ReactionTriggerSyntax declaration => declaration.Description,
        _ => null
    });

    static bool IsForModel(Catalog? candidate, ExecutableSemanticModel model)
    {
        if (candidate?.Entries is null || candidate.ModelRevision != model.Revision.ToString() || candidate.Entries.Count == 0)
        {
            return false;
        }

        var ids = DeclarationIds(model).Select(id => id.ToString()).ToHashSet(StringComparer.Ordinal);
        return candidate.Entries.All(entry => ids.Contains(entry.Key) && IsCanonical(entry.Value));
    }

    static bool IsCanonical(Metadata? metadata) => metadata is not null && !IsEmpty(metadata) &&
        metadata.Description == Normalize(metadata.Description) && metadata.Documentation == Normalize(metadata.Documentation);

    static bool IsEmpty(Metadata metadata) => metadata.Description is null && metadata.Documentation is null;

    static IEnumerable<SemanticId> DeclarationIds(ExecutableSemanticModel model) =>
        model.Application.Concepts.Select(concept => concept.Id).Concat(model.Application.Types.Select(type => type.Id))
            .Concat(model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices)
                .SelectMany(slice => slice.Events.Select(declaration => declaration.Id)
                    .Concat(slice.Commands.Select(declaration => declaration.Id))
                    .Concat(slice.ReadModels.Select(declaration => declaration.Id))
                    .Concat(slice.Queries.Select(declaration => declaration.Id))
                    .Concat(slice.Specifications.Select(declaration => declaration.Id))
                    .Concat((slice.Reactions.IsDefault ? [] : slice.Reactions).Select(declaration => declaration.Id))));

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static string? Normalize(string? text) => text is null ? null : XmlConvert.VerifyXmlChars(CSharpCodeBuilder.NormalizeDocumentation(text));

    internal sealed record Metadata(string? Description, string? Documentation)
    {
        internal CSharpCodeBuilder Render(CSharpCodeBuilder builder, string? fallbackSummary = null) =>
            builder.Documentation(Description, Documentation, fallbackSummary);
    }

    internal sealed record Catalog(string ModelRevision, SortedDictionary<string, Metadata> Entries);
}
