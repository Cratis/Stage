// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Renders the module that hands a generated application's Stage runtime what a live Stage serves over HTTP: the
/// Scene document, the routes of the modeled commands and queries, and the localized strings.
/// </summary>
/// <remarks>
/// Each route is read from the generated Arc proxy when the application starts, so it is always the route the
/// backend registered - never a planning-time guess at Arc's routing convention. The proxies are build output of
/// the Debug backend build, as they already are for the binding module.
/// </remarks>
internal static class StageModuleRenderer
{
    /// <summary>
    /// The planned artifact path.
    /// </summary>
    public const string RelativePath = "src/stage.ts";

    /// <summary>
    /// Renders the module.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slices">The slices whose generated proxies are present, in model order, each placed under the domain folder its code was generated in; only their command routes are read.</param>
    /// <returns>The generated module text.</returns>
    public static string Render(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var commands = slices.SelectMany(slice => slice.Slice.Commands.Select(command => (command.Name, Export: command.Name, Module: Module(slice))))
            .DistinctBy(command => command.Name, StringComparer.Ordinal)
            .OrderBy(command => command.Name, StringComparer.Ordinal)
            .Select((command, index) => (command.Name, command.Export, command.Module, Alias: $"__stageCommand{index}"))
            .ToArray();
        var builder = new StringBuilder()
            .Append("// What the Stage runtime renders this application from - the counterpart of a live Stage's\n")
            .Append("// /stage/scene, /stage/routes and /stage/strings endpoints. Generated; do not edit.\n")
            .Append("import type { StageSceneApplication, StageStaticContent } from '@cratis/stage.runtime';\n")
            .Append("import scene from './stage-scene.json';\n")
            .Append("import './bindings';\n");
        foreach (var (_, export, module, alias) in commands)
        {
            builder.Append("import { ").Append(export).Append(" as ").Append(alias).Append(" } from '").Append(module).Append("';\n");
        }

        builder.Append('\n')
            .Append("export const stage: StageStaticContent = {\n")
            .Append("    scene: scene as unknown as StageSceneApplication,\n")
            .Append("    routes: {\n")
            .Append("        commands: {\n");
        foreach (var (name, _, _, alias) in commands)
        {
            builder.Append("            ").Append(JsonSerializer.Serialize(name)).Append(": new ").Append(alias).Append("().route,\n");
        }

        builder.Append("        },\n")
            .Append("    },\n")
            .Append("    strings: ").Append(Strings(context)).Append(",\n")
            .Append("};\n");

        return builder.ToString();
    }

    // The default locale first, as the first locale a live Stage reports becomes the default, then the others.
    static string Strings(SemanticApplicationContext context)
    {
        if (context.Strings is not { } catalog)
        {
            return "{}";
        }

        var locales = catalog.Locales.Keys
            .OrderBy(locale => locale == catalog.DefaultLocale ? 0 : 1)
            .ThenBy(locale => locale, StringComparer.Ordinal);
        var entries = locales.Select(locale => $"{JsonSerializer.Serialize(locale)}: {JsonSerializer.Serialize(catalog.Locales[locale])}");
        return $"{{ {string.Join(", ", entries)} }}";
    }

    static string Module(LocatedSemanticSlice slice) =>
        "../" + string.Join('/', SliceNaming.FolderPath(slice.Path));
}
