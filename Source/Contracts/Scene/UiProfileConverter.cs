// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Scene.Model.SizeClasses;
using SceneModel = Cratis.Scene.Model.Profiles;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.UiProfileSyntax"/> into one
/// <see cref="SceneModel.UiProfile"/> per targeted platform - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// Screenplay declares a single <c language="csharp">ui profile</c> for potentially several platforms
/// (<c language="csharp">target platform web, ios</c>) and a single bare <c language="csharp">target size</c> name, while Scene's
/// <see cref="SceneModel.UiProfile"/> targets exactly one platform and a two-axis
/// <see cref="SizeClass"/>. This converter resolves both gaps deliberately: one <see cref="SceneModel.UiProfile"/>
/// is produced per platform - a platform is a deployment target of its own, which is what
/// <see cref="RenderPlanner"/> plans against (Cratis/Stage#39). A <c language="csharp">target size</c> is carried as a
/// <see cref="TargetSizeClass"/> rather than as a point in the two-axis arrangement matrix: what a target
/// assumes is a coarser statement than what an arrangement resolves against, and it has a third value the
/// matrix does not.
/// <see cref="ScreenplaySyntax.UiProfileSyntax.Layout"/> - the shell the profile selects - and
/// <see cref="ScreenplaySyntax.UiProfileSyntax.Theme"/> both carry straight through, and are what a render plan
/// resolves the target's layout and theme from. The selected layout is <em>not</em> what resolves
/// <see cref="Cratis.Scene.Model.Screens.Screen.Layout"/>; see <see cref="ScreenplaySceneVisitor"/> for what
/// does, and <see cref="RenderFindingKind.ScreenNotOnSelectedLayout"/> for how the difference is reported.
/// </remarks>
public static class UiProfileConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.UiProfileSyntax"/> into one <see cref="SceneModel.UiProfile"/> per platform.
    /// </summary>
    /// <param name="uiProfile">The <see cref="ScreenplaySyntax.UiProfileSyntax"/> to convert.</param>
    /// <returns>One <see cref="SceneModel.UiProfile"/> per platform in <see cref="ScreenplaySyntax.UiProfileSyntax.Platforms"/>.</returns>
    public static IEnumerable<SceneModel.UiProfile> Convert(ScreenplaySyntax.UiProfileSyntax uiProfile)
    {
        var defaultSizeClass = ConvertDefaultSizeClass(uiProfile.DefaultSizeClass);
        var packages = uiProfile.Packages.ToList();

        return uiProfile.Platforms.Select(platform =>
            new SceneModel.UiProfile(
                uiProfile.Name,
                platform,
                packages,
                defaultSizeClass,
                uiProfile.Layout,
                uiProfile.Theme,
                SampleData: SampleDataConverter.Selection(uiProfile.SampleData),
                Root: ConvertRoot(uiProfile.Root),
                Appearance: ConvertAppearance(uiProfile.Appearance),
                Defaults: ConvertDefaults(uiProfile.NewScreenTemplate)));
    }

    static SceneModel.RootTemplate? ConvertRoot(ScreenplaySyntax.RootTemplateSyntax? root)
    {
        if (root is null)
        {
            return null;
        }

        var options = root.Options.ToDictionary(option => option.Name, option => ConvertOption(option), StringComparer.Ordinal);
        return new SceneModel.RootTemplate(
            TemplateReferenceConverter.Reference(root.Name, root.Blueprint, root.Version),
            options.Count == 0 ? null : options);
    }

    // The value keeps the kind it was written with: a flag is a boolean and a number a number, so a shell reads `true`, not "true".
    static JsonNode? ConvertOption(ScreenplaySyntax.ShellOptionSyntax option) => option.Kind switch
    {
        ScreenplaySyntax.ShellOptionValueKind.Flag => JsonValue.Create(string.Equals(option.Value, "true", StringComparison.Ordinal)),
        ScreenplaySyntax.ShellOptionValueKind.Number => long.TryParse(option.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole)
            ? JsonValue.Create(whole)
            : JsonValue.Create(double.Parse(option.Value, NumberStyles.Float, CultureInfo.InvariantCulture)),
        _ => JsonValue.Create(option.Value)
    };

    static SceneModel.ProfileAppearance? ConvertAppearance(ScreenplaySyntax.ProfileAppearanceSyntax? appearance) =>
        appearance is null
            ? null
            : new SceneModel.ProfileAppearance(
                appearance.ColorScheme is null ? null : ParseColorScheme(appearance.ColorScheme),
                appearance.Accent);

    static SceneModel.ProfileDefaults? ConvertDefaults(ScreenplaySyntax.NewScreenTemplateSyntax? newScreenTemplate) =>
        newScreenTemplate is null
            ? null
            : new SceneModel.ProfileDefaults(TemplateReferenceConverter.Reference(newScreenTemplate.Name, newScreenTemplate.Blueprint, newScreenTemplate.Version));

    static SceneModel.ColorScheme ParseColorScheme(string name) =>
        Enum.TryParse<SceneModel.ColorScheme>(name, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? value
            : throw new UnknownColorSchemeName(name);

    static TargetSizeClass? ConvertDefaultSizeClass(string? defaultSizeClass) =>
        defaultSizeClass is null ? null : SizeClassNames.ParseTarget(defaultSizeClass);
}
