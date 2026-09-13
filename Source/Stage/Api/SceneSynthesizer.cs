// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Api;

/// <summary>
/// Builds the Scene an event model implies when the Screenplay document declares no <c language="csharp">screen</c> of its own.
/// </summary>
/// <remarks>
/// <para>
/// A model authored on Studio's canvas carries commands, read models and queries, but no screens - the canvas
/// has no way to record one. Rendering nothing for it is honest but useless: the person who pressed play modeled
/// an application and wants to see it. So the shape the model already states is turned into screens: one per
/// slice, showing what that slice's read model holds and what its command can do.
/// </para>
/// <para>
/// This is a default, never an override. A document that declares its own screens keeps them exactly as written -
/// <see cref="ScreenplaySceneVisitor"/> owns that translation, and nothing here runs.
/// </para>
/// <para>
/// Every element carries the modeled artifact's CLR type name, which is what lets the host attach the route Arc
/// actually registered for it. Guessing a route from naming conventions
/// would be a second answer to a question Arc already answers, and the two would drift.
/// </para>
/// </remarks>
public static class SceneSynthesizer
{
    /// <summary>
    /// The property every synthesized element carries the modeled artifact's emitted type name in.
    /// </summary>
    public const string TypeNameProperty = "typeName";

    /// <summary>
    /// The property the host writes the resolved API route into.
    /// </summary>
    public const string RouteProperty = "route";

    /// <summary>
    /// The property a command element carries its JSON schema in, so a frontend can build its form.
    /// </summary>
    public const string SchemaProperty = "schema";

    /// <summary>
    /// Synthesizes the screens an event model implies.
    /// </summary>
    /// <param name="scene">The translated scene, which decides whether anything is synthesized at all.</param>
    /// <param name="model">The event model to derive screens from.</param>
    /// <returns>The scene to serve: the translated one when it declares screens, otherwise one with synthesized screens.</returns>
    public static SceneApplication Synthesize(SceneApplication scene, EventModel model)
    {
        if (scene.Screens.Count > 0)
        {
            return scene;
        }

        var layout = scene.Layouts.Count > 0 ? scene.Layouts[0] : DefaultLayout.Create();
        var layouts = scene.Layouts.Count > 0 ? scene.Layouts : [layout];
        var screens = new List<SceneScreens.Screen>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var located in StageModelWalker.Slices(model))
        {
            var content = ContentFor(located);
            if (content.Count == 0)
            {
                continue;
            }

            var name = UniqueName(located, names);
            screens.Add(new SceneScreens.Screen(
                name,
                layout.Name,
                new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
                {
                    [DefaultLayout.ContentSlotName] = content
                },
                [],
                []));
        }

        return scene with { Layouts = layouts, Screens = screens };
    }

    static string UniqueName(LocatedSlice located, HashSet<string> taken)
    {
        var name = located.Slice.Name;
        if (taken.Add(name))
        {
            return name;
        }

        // Two slices in different features may share a name. The type namespace is unique by construction,
        // so its trailing segments disambiguate without inventing a numbering scheme nothing else knows.
        var qualified = string.Join('.', located.TypeNamespace.Split('.').TakeLast(2));
        var candidate = qualified;
        var suffix = 2;
        while (!taken.Add(candidate))
        {
            candidate = $"{qualified} ({suffix++})";
        }

        return candidate;
    }

    static List<SceneElements.SceneElement> ContentFor(LocatedSlice located)
    {
        var slice = located.Slice;
        var content = new List<SceneElements.SceneElement>
        {
            SceneElementFactory.Component($"{located.TypeNamespace}.title", "core:title", new Dictionary<string, object?>
            {
                ["text"] = Humanize(slice.Name)
            })
        };

        if (slice.ReadModel is { } readModel)
        {
            var typeName = $"{located.TypeNamespace}.{ModelNaming.ToIdentifier(readModel.Name)}";
            content.Add(SceneElementFactory.Component(
                $"{located.TypeNamespace}.data",
                "core:data",
                new Dictionary<string, object?>
                {
                    ["modelName"] = readModel.Name,
                    ["isCollection"] = true,
                    [TypeNameProperty] = typeName
                }));

            content.Add(SceneElementFactory.Component(
                $"{located.TypeNamespace}.table",
                "core:table",
                new Dictionary<string, object?>
                {
                    ["target"] = readModel.Name,
                    [TypeNameProperty] = typeName
                },
                new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
                {
                    ["columns"] = Columns(located.TypeNamespace, readModel.Schema)
                }));
        }

        if (slice.Command is { } command)
        {
            var typeName = $"{located.TypeNamespace}.{ModelNaming.ToIdentifier(command.Name)}";
            content.Add(SceneElementFactory.Component(
                $"{located.TypeNamespace}.action",
                "core:action",
                new Dictionary<string, object?>
                {
                    ["command"] = command.Name,
                    ["label"] = Humanize(command.Name),
                    [TypeNameProperty] = typeName,
                    [SchemaProperty] = command.Schema
                }));
        }

        // A slice with neither a read model nor a command (an automation, say) has nothing to show beyond its
        // own name, and a page holding only a heading is noise in the navigation.
        return content.Count > 1 ? content : [];
    }

    static IReadOnlyList<SceneElements.SceneElement> Columns(string typeNamespace, string schema)
    {
        var properties = SchemaProperties(schema);
        return
        [
            .. properties.Select(property => (SceneElements.SceneElement)SceneElementFactory.Component(
                $"{typeNamespace}.column.{property}",
                "core:column",
                new Dictionary<string, object?>
                {
                    ["property"] = property,
                    ["label"] = Humanize(property)
                }))
        ];
    }

    static IReadOnlyList<string> SchemaProperties(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            if (!document.RootElement.TryGetProperty("properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return [.. properties.EnumerateObject().Select(property => property.Name)];
        }
        catch (JsonException)
        {
            // A schema that does not parse says nothing about the shape; an empty table is better than a
            // failed render of the whole application.
            return [];
        }
    }

    static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var text = new System.Text.StringBuilder(value.Length * 2);
        text.Append(char.ToUpperInvariant(value[0]));
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                text.Append(' ');
            }

            text.Append(character);
        }

        return text.ToString();
    }
}
