// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Carries declaring-slice ownership and the translated screen objects from one compilation.
/// </summary>
internal static class ScreenPartitionsInput
{
    internal const string Name = "cratis-authoring:screen-partitions";
    const string Version = "1";

    internal static ArtifactRenderInput Create(ArtifactRenderProfile profile, SemanticCompilation compilation)
    {
        var scene = profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        if (scene is not null && !SceneCompositionAdmission.Matches(scene, out var mismatch))
        {
            throw new InvalidArtifactRenderContract(mismatch);
        }

        var documents = compilation.Documents;
        var workspace = ScreenplayWorkspace.Create(
            documents.IdentityCatalog.Application,
            compilation.Model.Application.Name,
            [.. documents.Documents.Select(document => WorkspaceDocument.Create(document.Id, document.StableKey, PortablePlayPath.Parse(document.DisplayPath), Encoding.UTF8.GetBytes(document.Text)))],
            documents.IdentityCatalog);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Diagnostics.Any(diagnostic => diagnostic.Severity == Screenplay.Diagnostics.DiagnosticSeverity.Error) ||
            workspace.Compilation.Value?.Model.Revision != compilation.Model.Revision)
        {
            throw new InvalidArtifactRenderContract("Screen partitions require the syntax and identity catalog from the same compilation.");
        }

        var request = new ArtifactRenderRequest(compilation.Model, Screenplay.Semantics.Execution.SemanticExecutionPlan.Compile(compilation.Model).Plan!, profile, new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Screens", "Screens"));
        var declarations = index.Entries.Where(entry => entry.Node is SliceSyntax && entry.SemanticId is not null)
            .SelectMany(entry => ((SliceSyntax)entry.Node).Screens.Select(screen =>
                (Owner: entry.SemanticId!.Value, Screen: screen, Path: Hierarchy(context.Slice(entry.SemanticId.Value)))))
            .ToList();
        if (scene is null)
        {
            if (declarations.Count > 0)
            {
                throw new InvalidArtifactRenderContract("Screen partitions require the translated Scene covering the authored screens.");
            }

            return ArtifactRenderInput.Create(Name, Version, [.. Serialize(new(compilation.Model.Revision.ToString(), string.Empty, []))]);
        }

        var root = JsonNode.Parse(scene.Bytes.AsSpan())!.AsObject();
        var screens = root["screens"]!.AsArray();
        var entries = new List<Entry>();
        for (var position = 0; position < screens.Count; position++)
        {
            var screen = screens[position]!.AsObject();
            var matches = declarations.Where(declaration => declaration.Screen.Name == screen["name"]!.GetValue<string>() &&
                declaration.Path.SequenceEqual(screen["path"]!.AsArray().Select(segment => segment!.GetValue<string>()))).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidArtifactRenderContract($"Screen '{screen["name"]}' has duplicate or unresolved declaring-slice ownership.");
            }

            var match = matches[0];
            declarations.Remove(match);
            var own = screen.DeepClone().AsObject();
            var contributions = own["contributions"]!.AsArray();
            var inheritedCount = contributions.Count - (match.Screen.Contributions?.Count() ?? 0);
            if (inheritedCount < 0)
            {
                throw new InvalidArtifactRenderContract($"Screen '{match.Screen.Name}' contributions disagree with the compilation.");
            }

            var inherited = new JsonArray([.. contributions.Take(inheritedCount).Select(value => value?.DeepClone())]);
            own["contributions"] = new JsonArray([.. contributions.Skip(inheritedCount).Select(value => value?.DeepClone())]);
            entries.Add(new(match.Owner.ToString(), position, own, inherited));
        }

        if (declarations.Count > 0)
        {
            throw new InvalidArtifactRenderContract("The translated Scene does not cover every authored screen in the compilation.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. Serialize(new(compilation.Model.Revision.ToString(), scene.Sha256, entries))]);
    }

    internal static bool TryRead(ArtifactRenderProfile profile, ExecutableSemanticModel? model, out Catalog? catalog, out string mismatch)
    {
        catalog = null;
        mismatch = string.Empty;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0)
        {
            return true;
        }

        if (inputs.Length != 1 || inputs[0].Version != Version)
        {
            mismatch = "Screen partitions have a duplicate input or unsupported version.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(inputs[0].Bytes.AsMemory());
            if (SceneCompositionAdmission.HasDuplicateMembers(document.RootElement))
            {
                mismatch = "Screen partitions contain duplicate JSON members.";
                return false;
            }

            var candidate = JsonSerializer.Deserialize<Catalog>(inputs[0].Bytes.AsSpan());
            var scene = profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
            if (candidate?.Entries is null || candidate.SceneSha256 != (scene?.Sha256 ?? string.Empty) ||
                (model is not null && candidate.ModelRevision != model.Revision.ToString()))
            {
                mismatch = "Screen partitions are stale or do not match the composed Scene payload and model revision.";
                return false;
            }

            if (scene is null)
            {
                if (candidate.Entries.Count != 0 || !inputs[0].Bytes.SequenceEqual(Serialize(candidate)))
                {
                    mismatch = "Screen partitions without an authored Scene must contain no screen records.";
                    return false;
                }

                catalog = candidate;
                return true;
            }

            var root = JsonNode.Parse(scene.Bytes.AsSpan())!;
            if (!scene.Bytes.SequenceEqual(Encoding.UTF8.GetBytes(CanonicalSceneJson.Write(root))))
            {
                mismatch = "Screen partitions require canonical composed Scene bytes.";
                return false;
            }

            if (Contracts.Scene.SceneGuards.HasUnevaluableGuard(Encoding.UTF8.GetString(scene.Bytes.AsSpan())))
            {
                mismatch = "Screen partitions contain a guard the Stage runtime cannot evaluate.";
                return false;
            }

            var screens = root["screens"]!.AsArray();
            var owners = model?.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices).Select(slice => slice.Id.ToString()).ToHashSet(StringComparer.Ordinal);
            if (candidate.Entries.Count != screens.Count || !candidate.Entries.Select(entry => entry.Index).SequenceEqual(Enumerable.Range(0, screens.Count)) ||
                candidate.Entries.Exists(entry => string.IsNullOrWhiteSpace(entry.Owner) || owners?.Contains(entry.Owner) == false || entry.Screen is null || entry.InheritedContributions is null))
            {
                mismatch = "Screen partitions contain duplicate, missing, malformed, or unresolved screen ownership.";
                return false;
            }

            if (candidate.Entries.Select(entry => ScreenKey(entry.Screen)).Distinct(StringComparer.Ordinal).Count() != screens.Count ||
                candidate.Entries.Exists(entry => CanonicalSceneJson.Write(Reassemble(entry)) != CanonicalSceneJson.Write(screens[entry.Index])) ||
                !inputs[0].Bytes.SequenceEqual(Serialize(candidate)))
            {
                mismatch = "Screen partitions disagree with the composed screen records or are not canonical.";
                return false;
            }

            catalog = candidate;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or NullReferenceException)
        {
            mismatch = "Screen partitions are malformed JSON or contain malformed screen records.";
            return false;
        }
    }

    internal static string Compose(ArtifactRenderInput scene, Catalog catalog)
    {
        var root = JsonNode.Parse(scene.Bytes.AsSpan())!.AsObject();
        root["screens"] = new JsonArray([.. catalog.Entries.Select(Reassemble)]);

        return CanonicalSceneJson.Write(root);
    }

    internal static IEnumerable<PlannedArtifact> Render(Catalog catalog, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        foreach (var slice in slices)
        {
            var entries = catalog.Entries.Where(entry => entry.Owner == slice.Slice.Id.ToString()).ToArray();
            if (entries.Length > 0)
            {
                var fragment = new JsonObject { ["screens"] = new JsonArray(entries.Select(entry => entry.Screen.DeepClone()).ToArray()) };
                yield return PlannedArtifact.CreateText($"{string.Join('/', SliceNaming.FolderPath(slice.Path))}/Screens.scene.json", CanonicalSceneJson.Write(fragment), [slice.Slice.Id]);
            }
        }
    }

    static JsonObject Reassemble(Entry entry)
    {
        var screen = entry.Screen.DeepClone().AsObject();
        screen["contributions"] = new JsonArray([.. entry.InheritedContributions.Concat(screen["contributions"]!.AsArray()).Select(value => value?.DeepClone())]);

        return screen;
    }

    static string ScreenKey(JsonObject screen) => CanonicalSceneJson.Write(new JsonArray(screen["path"]?.DeepClone(), screen["name"]?.DeepClone()));
    static string[] Hierarchy(LocatedSemanticSlice slice) => [slice.Module.Name, .. slice.FeaturePath.Select(feature => feature.Name)];
    static byte[] Serialize(Catalog catalog) => Encoding.UTF8.GetBytes(CanonicalSceneJson.Write(JsonSerializer.SerializeToNode(catalog)));
    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    internal sealed record Entry(string Owner, int Index, JsonObject Screen, JsonArray InheritedContributions);
    internal sealed record Catalog(string ModelRevision, string SceneSha256, List<Entry> Entries);
}
