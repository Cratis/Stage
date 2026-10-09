// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Semantics;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Api;

/// <summary>
/// Builds the Scene a model implies when the Screenplay document declares no <c language="csharp">screen</c> of its own.
/// </summary>
/// <remarks>
/// A document that declares its own screens keeps them exactly as written. Otherwise, one screen per slice
/// shows its read model and command. Elements carry the artifact's emitted type name so the host can attach
/// the route actually registered for it rather than guessing a URL.
/// </remarks>
public static class SceneSynthesizer
{
    /// <summary>
    /// The property every synthesized element carries the modeled artifact's emitted type name in.
    /// </summary>
    public const string TypeNameProperty = SceneElementProperties.TypeName;

    /// <summary>
    /// The property the host writes the resolved API route into.
    /// </summary>
    public const string RouteProperty = SceneElementProperties.Route;

    /// <summary>
    /// The property a command element carries its JSON schema in, so a frontend can build its form.
    /// </summary>
    public const string SchemaProperty = SceneElementProperties.Schema;

    /// <summary>
    /// Synthesizes the screens an event model implies.
    /// </summary>
    /// <param name="scene">The translated scene, which decides whether anything is synthesized at all.</param>
    /// <param name="model">The event model to derive screens from.</param>
    /// <returns>The scene to serve: the translated one when it declares screens, otherwise one with synthesized screens.</returns>
    public static SceneApplication Synthesize(SceneApplication scene, EventModel model)
    {
        var slices = StageModelWalker.Slices(model).ToArray();
        var commands = slices
            .Where(located => located.Slice.Command is not null)
            .ToDictionary(
                located => located.Slice.Command!.Name,
                located => CommandFormRuntimeMetadata.FromSchema(located.Slice.Command!.Name, located.Slice.Command!.Schema),
                StringComparer.Ordinal);
        var enriched = CommandFormRuntimeMetadata.Apply(scene, commands);

        return Synthesize(
            enriched,
            slices.Select(located => new SynthesizedSlice(
                located.Slice.Name,
                located.TypeNamespace,
                SynthesizedSceneContent.For(
                    located.Slice.Name,
                    located.TypeNamespace,
                    located.Slice.ReadModel?.Name,
                    located.Slice.ReadModel?.Schema,
                    located.Slice.Command?.Name,
                    located.Slice.Command is null ? null : commands[located.Slice.Command.Name]))));
    }

    /// <summary>
    /// Synthesizes the screens an executable semantic model implies without a legacy event-model projection.
    /// </summary>
    /// <param name="scene">The translated authored presentation, preserved when it declares screens.</param>
    /// <param name="model">The executable semantic model to derive screens from.</param>
    /// <returns>The authored scene, or the same default screen composition used for an event model.</returns>
    public static SceneApplication Synthesize(SceneApplication scene, ExecutableSemanticModel model)
    {
        var schemas = new SemanticSceneSchemas(model.Application);
        var slices = SemanticModelWalker.Slices(model).ToArray();
        var readModels = slices.SelectMany(located => located.Slice.ReadModels).ToDictionary(readModel => readModel.Id);
        var commands = slices
            .SelectMany(located => located.Slice.Commands)
            .GroupBy(command => command.Name, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => CommandFormRuntimeMetadata.FromSchema(group.Key, schemas.ForProperties(group.First().Properties)),
                StringComparer.Ordinal);
        var enriched = CommandFormRuntimeMetadata.Apply(scene, commands);

        return Synthesize(enriched, slices.Select(located =>
        {
            var slice = located.Slice;
            var readModelId = slice.Projections.FirstOrDefault()?.ReadModel ?? slice.Queries.FirstOrDefault()?.ReadModel;
            var readModel = readModelId is { } id ? readModels[id] : null;
            var command = slice.Commands.FirstOrDefault();

            return new SynthesizedSlice(slice.Name, located.TypeNamespace, SynthesizedSceneContent.For(
                slice.Name,
                located.TypeNamespace,
                readModel?.Name,
                readModel is null ? null : SemanticSchemas.Schema(readModel.Properties, model.Application),
                command?.Name,
                command is null ? null : commands[command.Name]));
        }));
    }

    static SceneApplication Synthesize(SceneApplication scene, IEnumerable<SynthesizedSlice> slices)
    {
        if (scene.Screens.Count > 0)
        {
            return scene;
        }

        var layout = scene.Layouts.Count > 0 ? scene.Layouts[0] : DefaultLayout.Create();
        var layouts = scene.Layouts.Count > 0 ? scene.Layouts : [layout];
        var screens = new List<SceneScreens.Screen>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var slice in slices.Where(slice => slice.Content.Count > 0))
        {
            screens.Add(new SceneScreens.Screen(
                UniqueName(slice, names),
                layout.Name,
                new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
                {
                    [DefaultLayout.ContentSlotName] = slice.Content
                },
                [],
                []));
        }

        return scene with { Layouts = layouts, Screens = screens };
    }

    static string UniqueName(SynthesizedSlice slice, HashSet<string> taken)
    {
        if (taken.Add(slice.Name))
        {
            return slice.Name;
        }

        // Two slices in different features may share a name. The trailing namespace segments disambiguate them.
        var qualified = string.Join('.', slice.TypeNamespace.Split('.').TakeLast(2));
        var candidate = qualified;
        var suffix = 2;
        while (!taken.Add(candidate))
        {
            candidate = $"{qualified} ({suffix++})";
        }

        return candidate;
    }

    sealed record SynthesizedSlice(string Name, string TypeNamespace, IReadOnlyList<SceneElements.SceneElement> Content);
}
