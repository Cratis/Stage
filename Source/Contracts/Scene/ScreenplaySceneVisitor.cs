// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Visits a compiled Screenplay <see cref="ScreenplaySyntax.ApplicationSyntax"/> and produces the Scene
/// translation of it - part of Cratis/Stage#37. A separate, parallel visitor from
/// <see cref="Cratis.Stage.Contracts.Screenplay.ScreenplayEventModelVisitor"/> (which produces the unrelated <c>EventModel</c>) -
/// this one does not touch it.
/// </summary>
/// <remarks>
/// The roots follow Screenplay's taxonomy: <c>layout</c> is top-level (the application's one navigational
/// shell), while <c>screen template</c> and <c>dialog template</c> - the reusable shapes inside it - are
/// module-scoped. A document may declare several layouts so that different <c>ui profile</c>s can select
/// different shells; every screen resolves against the first declared one, because
/// <see cref="SceneScreens.Screen.Layout"/> holds a single name. Carrying the per-profile selection through
/// would mean one <see cref="SceneScreens.Screen"/> per profile - a deliberate gap, not attempted here.
/// </remarks>
public sealed class ScreenplaySceneVisitor : ScreenplaySyntax.IApplicationSyntaxVisitor<SceneApplication>
{
    /// <inheritdoc/>
    public SceneApplication Visit(ScreenplaySyntax.ApplicationSyntax syntax)
    {
        var uiProfiles = syntax.UiProfiles?.SelectMany(UiProfileConverter.Convert).ToList() ?? [];
        var themes = syntax.Themes?.Select(ThemeConverter.Convert).ToList() ?? [];
        var layouts = ConvertLayouts(syntax);

        var screenTemplates = new List<SceneScreens.ScreenTemplate>();
        var dialogTemplates = new List<SceneScreens.DialogTemplate>();
        var screens = new List<SceneScreens.Screen>();

        foreach (var module in syntax.Modules)
        {
            screenTemplates.AddRange((module.ScreenTemplates ?? []).Select(ScreenTemplateConverter.Convert));
            dialogTemplates.AddRange((module.DialogTemplates ?? []).Select(DialogTemplateConverter.Convert));
            ConvertModuleScreens(module, layouts[0].Name, screens);
        }

        return new SceneApplication(uiProfiles, themes, layouts, screenTemplates, dialogTemplates, screens);
    }

    static List<SceneLayouts.Layout> ConvertLayouts(ScreenplaySyntax.ApplicationSyntax syntax)
    {
        var layouts = (syntax.Layouts ?? []).Select(LayoutConverter.Convert).ToList();
        return layouts.Count > 0 ? layouts : [DefaultLayout.Create()];
    }

    static void ConvertModuleScreens(ScreenplaySyntax.ModuleSyntax module, string layoutName, List<SceneScreens.Screen> screens)
    {
        var forms = module.Forms?.ToList() ?? [];
        foreach (var feature in module.Features)
        {
            ConvertFeatureScreens(feature, module.Name, layoutName, forms, screens);
        }
    }

    static void ConvertFeatureScreens(
        ScreenplaySyntax.FeatureSyntax feature,
        string featurePath,
        string layoutName,
        IReadOnlyList<ScreenplaySyntax.FormSyntax> forms,
        List<SceneScreens.Screen> screens)
    {
        var path = $"{featurePath}.{feature.Name}";
        var contributions = (feature.Contributions ?? [])
            .Select((contribution, index) => ContributionConverter.Convert(contribution, $"{path}.contribution[{index}]"))
            .ToList();

        screens.AddRange(feature.Slices
            .SelectMany(slice => slice.Screens)
            .Select(screen => ScreenConverter.Convert(screen, layoutName, forms, contributions)));

        foreach (var subFeature in feature.Features)
        {
            ConvertFeatureScreens(subFeature, path, layoutName, forms, screens);
        }
    }
}
