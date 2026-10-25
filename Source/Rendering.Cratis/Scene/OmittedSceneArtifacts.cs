// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

static class OmittedSceneArtifacts
{
    internal static void Repair(ArtifactRenderRequest request, List<PlannedArtifact> admitted, RenderingPartAdmission parts)
    {
        if (parts.OmittedOwners.Count == 0 || !CratisArtifactRenderProfileAdmission.Matches(request, out var options, out _)) return;

        var context = new SemanticApplicationContext(request, options);
        var names = Names(context, parts.OmittedOwners);
        var retained = parts.SceneContext(options);
        var slices = parts.SceneSlices(retained) ?? [];
        for (var index = 0; index < admitted.Count; index++)
        {
            var artifact = admitted[index];
            var content = Render(artifact, retained, slices, names);
            if (content is not null) admitted[index] = PlannedArtifact.CreateText(artifact.RelativePath, content, artifact.Sources);
        }
    }

    static HashSet<string> Names(SemanticApplicationContext context, IReadOnlySet<SemanticId> omitted) => context.Commands.Values.Where(command => omitted.Contains(command.Id)).Select(command => command.Name)
        .Concat(context.ReadModels.Values.Where(model => omitted.Contains(model.Id)).Select(model => model.Name))
        .Concat(context.Queries.Values.Where(query => omitted.Contains(query.Id) || omitted.Contains(query.ReadModel)).Select(query => query.Name))
        .Concat(context.AllSlices.Where(slice => omitted.Contains(slice.Slice.Id)).Select(slice => slice.Slice.Name))
        .Concat(omitted.Select(owner => owner.ToString())).ToHashSet(StringComparer.Ordinal);

    static string? Render(PlannedArtifact artifact, SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, HashSet<string> names) => artifact.RelativePath switch
    {
        StageModuleRenderer.RelativePath => StageModuleRenderer.Render(context, slices),
        SceneBindingsRenderer.RelativePath => SceneBindingsRenderer.Render(context, slices),
        StageSceneDocument.RelativePath or SceneCompositionInput.RelativePath => WithoutOwners(artifact, names),
        _ when artifact.RelativePath.EndsWith(".scene.json", StringComparison.Ordinal) => WithoutOwners(artifact, names),
        _ => null
    };

    static string WithoutOwners(PlannedArtifact artifact, HashSet<string> names)
    {
        var document = JsonNode.Parse(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))!.AsObject();
        RemoveReferences(document, names);
        return CanonicalSceneJson.Write(document);
    }

    static void RemoveReferences(JsonNode node, HashSet<string> names)
    {
        if (node is JsonArray array) RemoveArrayReferences(array, names);
        else if (node is JsonObject obj) RemoveObjectReferences(obj, names);
    }

    static void RemoveArrayReferences(JsonArray array, HashSet<string> names)
    {
        foreach (var child in array.ToArray())
        {
            if (ReferencesOwner(child, names)) array.Remove(child);
            else if (child is not null) RemoveReferences(child, names);
        }
    }

    static void RemoveObjectReferences(JsonObject obj, HashSet<string> names)
    {
        foreach (var (key, child) in obj.ToArray())
        {
            if (Matches(key, names) || ReferencesOwner(child, names)) obj.Remove(key);
            else if (child is not null) RemoveReferences(child, names);
        }
    }

    static bool ReferencesOwner(JsonNode? node, HashSet<string> names) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => Matches(text, names),
        JsonObject obj => obj.Any(property => property.Value is JsonValue value && value.TryGetValue<string>(out var text) && Matches(text, names)),
        _ => false
    };

    static bool Matches(string text, HashSet<string> names)
    {
        string[] references = [text, text[(text.LastIndexOf('.') + 1)..], text[(text.LastIndexOf(':') + 1)..], text.Split(':', 2)[0]];
        return references.Any(names.Contains);
    }
}
