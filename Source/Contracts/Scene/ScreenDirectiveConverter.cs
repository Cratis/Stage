// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneElements = Cratis.Scene.Model.Elements;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a Screenplay screen's <see cref="ScreenplaySyntax.ScreenDirectiveSyntax"/> tree into
/// <see cref="SceneElements.SceneElement"/>s - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// Every directive kind (including <c>data</c>, which is not itself visual) becomes one
/// <see cref="SceneElements.ExternalComponent"/> named <c>core:&lt;directive-kind&gt;</c>, with the
/// directive's own fields folded into its open <c>Properties</c> bag and any nested directives placed in
/// its <c>Slots["content"]</c>. This is a deliberately mechanical, uniform mapping rather than a bespoke
/// <c>SceneElement</c> subtype per widget - the <c>core:*</c> components don't have to exist in
/// <c>Scene.React</c> yet for the translated model to be correct; rendering them is a separate, later
/// concern (a <c>core</c> package addition), the same reasoning Cratis/Scene#5 used to leave
/// <c>Scene.React</c> untouched.
/// </remarks>
public static class ScreenDirectiveConverter
{
    /// <summary>
    /// Converts a sequence of sibling <see cref="ScreenplaySyntax.ScreenDirectiveSyntax"/> into
    /// <see cref="SceneElements.SceneElement"/>s.
    /// </summary>
    /// <param name="directives">The sibling directives to convert.</param>
    /// <param name="path">The id path of the directives' parent, used to derive unique element ids.</param>
    /// <returns>The converted elements, in declaration order.</returns>
    public static IReadOnlyList<SceneElements.SceneElement> Convert(IEnumerable<ScreenplaySyntax.ScreenDirectiveSyntax> directives, string path) =>
        [.. directives.Select((directive, index) => Convert(directive, $"{path}.{index}-{Kind(directive)}"))];

    static SceneElements.ExternalComponent Convert(ScreenplaySyntax.ScreenDirectiveSyntax directive, string id) =>
        directive switch
        {
            ScreenplaySyntax.ScreenDataSyntax data => SceneElementFactory.Component(id, "core:data", new Dictionary<string, object?>
            {
                ["typeName"] = data.Type.Name,
                ["isCollection"] = data.Type.IsCollection,
                ["query"] = data.Query,
                ["by"] = data.By,
            }),
            ScreenplaySyntax.ScreenActionSyntax action => SceneElementFactory.Component(id, "core:action", new Dictionary<string, object?>
            {
                ["command"] = action.Command,
                ["label"] = action.Label,
                ["navigateToScreen"] = action.Navigate?.Screen,
                ["navigateByParameter"] = action.Navigate?.By,
            }),
            ScreenplaySyntax.ScreenSectionSyntax section => SceneElementFactory.Component(
                id,
                "core:section",
                new Dictionary<string, object?> { ["name"] = section.Name },
                new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["content"] = Convert(section.Directives, id) }),
            ScreenplaySyntax.ScreenNavigateSyntax navigate => SceneElementFactory.Component(id, "core:navigate", new Dictionary<string, object?>
            {
                ["targetScreen"] = navigate.Screen,
                ["by"] = navigate.By,
            }),
            ScreenplaySyntax.ScreenTitleSyntax title => SceneElementFactory.Component(id, "core:title", new Dictionary<string, object?> { ["text"] = title.Text }),
            ScreenplaySyntax.ScreenTableSyntax table => ConvertTable(table, id),
            ScreenplaySyntax.ScreenSummarySyntax summary => ConvertSummary(summary, id),
            ScreenplaySyntax.ScreenCodeSyntax code => SceneElementFactory.Component(id, "core:code", new Dictionary<string, object?>
            {
                ["language"] = code.Code.Language,
                ["code"] = code.Code.Code,
            }),
            _ => throw new UnknownScreenDirective(directive.GetType().Name),
        };

    static SceneElements.ExternalComponent ConvertTable(ScreenplaySyntax.ScreenTableSyntax table, string id)
    {
        var properties = new Dictionary<string, object?>
        {
            ["target"] = table.Target,
            ["navigateOnRowClickToScreen"] = table.RowClick?.Screen,
            ["navigateOnRowClickByParameter"] = table.RowClick?.By,
        };

        var columns = table.Columns
            .Select((column, index) => (SceneElements.SceneElement)SceneElementFactory.Component(
                $"{id}.{index}-column",
                "core:column",
                new Dictionary<string, object?> { ["property"] = column.Property, ["label"] = column.Label }))
            .ToList();

        return SceneElementFactory.Component(id, "core:table", properties, new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["columns"] = columns });
    }

    static SceneElements.ExternalComponent ConvertSummary(ScreenplaySyntax.ScreenSummarySyntax summary, string id)
    {
        var fields = summary.Fields
            .Select((field, index) => (SceneElements.SceneElement)SceneElementFactory.Component(
                $"{id}.{index}-field",
                "core:field",
                new Dictionary<string, object?> { ["property"] = field.Property, ["label"] = field.Label }))
            .ToList();

        return SceneElementFactory.Component(
            id,
            "core:summary",
            new Dictionary<string, object?> { ["target"] = summary.Target },
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> { ["fields"] = fields });
    }

    static string Kind(ScreenplaySyntax.ScreenDirectiveSyntax directive) =>
        directive switch
        {
            ScreenplaySyntax.ScreenDataSyntax => "data",
            ScreenplaySyntax.ScreenActionSyntax => "action",
            ScreenplaySyntax.ScreenSectionSyntax => "section",
            ScreenplaySyntax.ScreenNavigateSyntax => "navigate",
            ScreenplaySyntax.ScreenTitleSyntax => "title",
            ScreenplaySyntax.ScreenTableSyntax => "table",
            ScreenplaySyntax.ScreenSummarySyntax => "summary",
            ScreenplaySyntax.ScreenCodeSyntax => "code",
            _ => throw new UnknownScreenDirective(directive.GetType().Name),
        };
}
