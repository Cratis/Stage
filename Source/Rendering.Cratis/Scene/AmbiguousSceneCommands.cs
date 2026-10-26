// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>Admits command references without interpreting arbitrary Scene payload data.</summary>
internal static class AmbiguousSceneCommands
{
    static readonly string[] _surfaceCollections = ["layouts", "screens", "screenTemplates", "dialogTemplates"];
    static readonly string[] _slotCollections = ["slotContent", "content"];
    static readonly string[] _outcomes = ["onSuccess", "onFailure", "onResult"];

    /// <summary>
    /// Refuses same-slice collisions and ambiguous bare command references in an authored Scene.
    /// </summary>
    /// <param name="context">The whole indexed application.</param>
    /// <param name="slices">The composed UI slices, or <c language="csharp">null</c> when the plan composes none.</param>
    /// <param name="sceneJson">The admitted authored Scene, if carried by the profile.</param>
    /// <param name="diagnostics">The plan diagnostics to add the refusals to.</param>
    public static void Refuse(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice>? slices, string? sceneJson, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slices is null)
        {
            return;
        }

        var references = sceneJson is null ? [] : References(JsonNode.Parse(sceneJson)).ToHashSet(StringComparer.Ordinal);
        var ambiguousReferences = context.SceneCommands.AmbiguousNames.Where(references.Contains).ToHashSet(StringComparer.Ordinal);
        var collisions = slices.SelectMany(SameSliceCollisions)
            .Concat(context.AllSlices.SelectMany(slice => slice.Slice.Commands.Where(command => ambiguousReferences.Contains(command.Name))
                .Select(command => (Slice: slice, command.Name))))
            .DistinctBy(collision => (collision.Slice.Slice.Id, collision.Name));
        diagnostics.AddRange(context.SceneCommands.Collisions.Select(entry => new ArtifactRenderDiagnostic(
            "STAGE-SCREEN-008",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Command '{entry.Command.Name}' has no unique structural Scene identity.",
            context.Application.Id)));
        diagnostics.AddRange(collisions.Select(collision => new ArtifactRenderDiagnostic(
            "STAGE-SCREEN-008",
            ArtifactRenderDiagnosticSeverity.Error,
            $"The command name '{collision.Name}' in slice '{QualifiedSlice(collision.Slice)}' cannot be resolved unambiguously; use distinct names within a slice and qualified slice ids in authored Scene command references.",
            context.Application.Id)));
    }

    internal static IEnumerable<string> References(JsonNode? application) =>
        application is JsonObject document
            ? Behaviors(document).Concat(_surfaceCollections
                .SelectMany(collection => Items(document[collection])).SelectMany(Surface))
            : [];

    static IEnumerable<(LocatedSemanticSlice Slice, string Name)> SameSliceCollisions(LocatedSemanticSlice slice) =>
        slice.Slice.Commands.GroupBy(command => command.Name, StringComparer.Ordinal)
            .Where(group => group.Select(command => command.Id).Distinct().Skip(1).Any())
            .Select(group => (slice, group.Key));

    static string QualifiedSlice(LocatedSemanticSlice slice) =>
        StructureConverter.QualifiedId([slice.Module.Name, .. slice.FeaturePath.Select(feature => feature.Name), slice.Slice.Name]);

    static IEnumerable<string> Surface(JsonNode? node) =>
        node is JsonObject surface
            ? Behaviors(surface)
                .Concat(Items(surface["forms"]).OfType<JsonObject>().SelectMany(form => Reference(form["forCommand"]).Concat(Behaviors(form))))
                .Concat(_slotCollections.SelectMany(slot => SlotElements(surface[slot])).SelectMany(Element))
                .Concat(Items(surface["contributions"]).OfType<JsonObject>().SelectMany(contribution => Element(contribution["content"])))
            : [];

    static IEnumerable<string> Element(JsonNode? node) =>
        node is JsonObject element
            ? Behaviors(element).Concat(Component(element))
                .Concat(Items(element["children"]).SelectMany(Element))
                .Concat(SlotElements(element["slots"]).SelectMany(Element))
            : [];

    static IEnumerable<string> Component(JsonObject element)
    {
        var properties = element["properties"] as JsonObject;
        return Text(element["componentName"]) switch
        {
            "Stage:commandForm" or "Cratis.Components:commandForm" or "core:commandForm" or "core:action" => Reference(properties?["command"])
                .Concat(Items(properties?["alternatives"]).Append(properties?["otherwise"]).OfType<JsonObject>().SelectMany(branch => Reference(branch["command"]))),
            "core:toolbarItem" when Text(properties?["kind"]) == "Action" => Reference(properties?["target"]),
            "Stage:confirmDialog" => Items(properties?["decisions"]).OfType<JsonObject>().SelectMany(decision => Reference(decision["command"])),
            _ => []
        };
    }

    static IEnumerable<JsonNode?> SlotElements(JsonNode? node) =>
        node is JsonObject slots ? slots.SelectMany(pair => Items(pair.Value)) : [];

    static IEnumerable<string> Behaviors(JsonObject owner) =>
        Items(owner["behaviors"]).OfType<JsonObject>().SelectMany(behavior => Items(behavior["bindings"]).OfType<JsonObject>())
            .SelectMany(binding => Actions(binding["actions"]));

    static IEnumerable<string> Actions(JsonNode? actions) => Items(actions).OfType<JsonObject>().SelectMany(action =>
        (Text(action["kind"]) == "ExecuteCommand" ? Reference(action["command"]) : [])
            .Concat(_outcomes.SelectMany(outcome => Actions(action[outcome]))));

    static IEnumerable<string> Reference(JsonNode? node) => Text(node) is { } text ? [text] : [];
    static JsonArray Items(JsonNode? node) => node as JsonArray ?? [];
    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
