// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
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
    const string Version = "2";

    internal static ArtifactRenderInput Create(ArtifactRenderProfile profile, SemanticCompilation compilation)
    {
        var scene = profile.Inputs.SingleOrDefault(input => input.Name == $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}");
        if (scene is not null && !SceneCompositionAdmission.Matches(scene, out var mismatch))
        {
            throw new InvalidArtifactRenderContract(mismatch);
        }

        var authority = ScreenCompilationAuthority.Capture(compilation);
        var workspace = ScreenCompilationAuthority.Open(authority, compilation.Model.Revision.ToString());
        var declarations = ScreenCompilationAuthority.Declarations(workspace);
        var compiledScreens = ScreenCompilationAuthority.Screens(workspace);
        if (scene is null)
        {
            if (declarations.Count > 0)
            {
                throw new InvalidArtifactRenderContract("Screen partitions require the translated Scene covering the authored screens.");
            }

            return ArtifactRenderInput.Create(Name, Version, [.. Serialize(new(compilation.Model.Revision.ToString(), string.Empty, [], authority))]);
        }

        var root = JsonNode.Parse(scene.Bytes.AsSpan())!.AsObject();
        var screens = root["screens"]!.AsArray();
        var entries = new List<Entry>();
        for (var position = 0; position < screens.Count; position++)
        {
            var screen = screens[position]!.AsObject();
            var key = ScreenCompilationAuthority.Key(screen);
            var matches = declarations.Where(declaration => declaration.Key == key).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidArtifactRenderContract($"Screen '{screen["name"]}' has duplicate or unresolved declaring-slice ownership.");
            }

            RequirePayloadAgreement(screen, compiledScreens);
            var match = matches[0];
            declarations.Remove(match);
            var own = screen.DeepClone().AsObject();
            var contributions = own["contributions"]!.AsArray();
            var inheritedCount = contributions.Count - match.OwnContributions;
            if (inheritedCount < 0)
            {
                throw new InvalidArtifactRenderContract($"Screen '{screen["name"]}' contributions disagree with the compilation.");
            }

            var inherited = new JsonArray([.. contributions.Take(inheritedCount).Select(value => value?.DeepClone())]);
            own["contributions"] = new JsonArray([.. contributions.Skip(inheritedCount).Select(value => value?.DeepClone())]);
            entries.Add(new(match.Owner, position, own, inherited));
        }

        if (declarations.Count > 0)
        {
            throw new InvalidArtifactRenderContract("The translated Scene does not cover every authored screen in the compilation.");
        }

        return ArtifactRenderInput.Create(Name, Version, [.. Serialize(new(compilation.Model.Revision.ToString(), scene.Sha256, entries, authority))]);
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
            if (candidate?.Entries is null || candidate.Authority is null || candidate.SceneSha256 != (scene?.Sha256 ?? string.Empty) ||
                (model is not null && candidate.ModelRevision != model.Revision.ToString()))
            {
                mismatch = "Screen partitions are stale or do not match the composed Scene payload and model revision.";
                return false;
            }

            var workspace = ScreenCompilationAuthority.Open(candidate.Authority, candidate.ModelRevision);
            var declarations = ScreenCompilationAuthority.Declarations(workspace);
            var compiledScreens = ScreenCompilationAuthority.Screens(workspace);
            if (scene is null)
            {
                if (candidate.Entries.Count != 0 || declarations.Count != 0 || !inputs[0].Bytes.SequenceEqual(Serialize(candidate)))
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

            foreach (var entry in candidate.Entries)
            {
                var key = ScreenCompilationAuthority.Key(entry.Screen);
                if (!declarations.Exists(declaration => declaration.Owner == entry.Owner && declaration.Key == key))
                {
                    mismatch = $"Screen partitions name slice '{entry.Owner}' as owner of screen '{entry.Screen["name"]}', but that slice does not declare the screen in the compilation.";
                    return false;
                }

                RequirePayloadAgreement(Reassemble(entry), compiledScreens);
            }

            if (candidate.Entries.Count != declarations.Count ||
                candidate.Entries.Select(entry => ScreenCompilationAuthority.Key(entry.Screen)).Distinct(StringComparer.Ordinal).Count() != screens.Count ||
                candidate.Entries.Exists(entry => CanonicalSceneJson.Write(Reassemble(entry)) != CanonicalSceneJson.Write(screens[entry.Index])) ||
                !inputs[0].Bytes.SequenceEqual(Serialize(candidate)))
            {
                mismatch = "Screen partitions disagree with the composed screen records or are not canonical.";
                return false;
            }

            catalog = candidate;
            return true;
        }
        catch (InvalidArtifactRenderContract exception)
        {
            mismatch = exception.Message;
            return false;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or NullReferenceException or InvalidSemanticContract or Screenplay.Workspaces.InvalidScreenplayWorkspace)
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

    static void RequirePayloadAgreement(JsonObject screen, JsonArray compiledScreens)
    {
        var key = ScreenCompilationAuthority.Key(screen);
        var matches = compiledScreens.OfType<JsonObject>().Where(compiled => ScreenCompilationAuthority.Key(compiled) == key).ToArray();
        if (matches.Length != 1 || CanonicalSceneJson.Write(screen) != CanonicalSceneJson.Write(matches[0]))
        {
            throw new InvalidArtifactRenderContract($"Screen partitions carry a stale or mismatched payload for screen '{screen["name"]}' compared with the compiled Scene.");
        }
    }

    static byte[] Serialize(Catalog catalog) => Encoding.UTF8.GetBytes(CanonicalSceneJson.Write(JsonSerializer.SerializeToNode(catalog)));
    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    internal sealed record Entry(string Owner, int Index, JsonObject Screen, JsonArray InheritedContributions);
    internal sealed record Catalog(string ModelRevision, string SceneSha256, List<Entry> Entries, ScreenCompilationAuthority.Source Authority);
}
