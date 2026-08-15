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
public sealed class ScreenplaySceneVisitor : ScreenplaySyntax.IApplicationSyntaxVisitor<SceneApplication>
{
    /// <inheritdoc/>
    public SceneApplication Visit(ScreenplaySyntax.ApplicationSyntax syntax)
    {
        var uiProfiles = syntax.UiProfiles?.SelectMany(UiProfileConverter.Convert).ToList() ?? [];
        var themes = syntax.Themes?.Select(ThemeConverter.Convert).ToList() ?? [];

        var layouts = new List<SceneLayouts.Layout>();
        var screens = new List<SceneScreens.Screen>();

        foreach (var module in syntax.Modules)
        {
            ConvertModule(module, layouts, screens);
        }

        return new SceneApplication(uiProfiles, themes, layouts, screens);
    }

    static void ConvertModule(ScreenplaySyntax.ModuleSyntax module, List<SceneLayouts.Layout> layouts, List<SceneScreens.Screen> screens)
    {
        layouts.AddRange(module.Layouts.Select(LayoutConverter.Convert));

        var forms = module.Forms?.ToList() ?? [];
        foreach (var feature in module.Features)
        {
            ConvertFeature(feature, $"{module.Name}", forms, layouts, screens);
        }
    }

    static void ConvertFeature(
        ScreenplaySyntax.FeatureSyntax feature,
        string featurePath,
        IReadOnlyList<ScreenplaySyntax.FormSyntax> forms,
        List<SceneLayouts.Layout> layouts,
        List<SceneScreens.Screen> screens)
    {
        var path = $"{featurePath}.{feature.Name}";
        var contributions = (feature.Contributions ?? [])
            .Select((contribution, index) => ContributionConverter.Convert(contribution, $"{path}.contribution[{index}]"))
            .ToList();

        foreach (var slice in feature.Slices)
        {
            foreach (var screen in slice.Screens)
            {
                var result = ScreenConverter.Convert(screen, forms, contributions);
                screens.Add(result.Screen);
                if (result.ImplicitLayout is not null)
                {
                    layouts.Add(result.ImplicitLayout);
                }
            }
        }

        foreach (var subFeature in feature.Features)
        {
            ConvertFeature(subFeature, path, forms, layouts, screens);
        }
    }
}
