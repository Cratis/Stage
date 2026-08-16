// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SizeClasses;
using SceneModel = Cratis.Scene.Model.Profiles;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.UiProfileSyntax"/> into one
/// <see cref="SceneModel.UiProfile"/> per targeted platform - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// Screenplay declares a single <c>ui profile</c> for potentially several platforms
/// (<c>target platform web, ios</c>) and a single bare <c>target size</c> name, while Scene's
/// <see cref="SceneModel.UiProfile"/> targets exactly one platform and a two-axis
/// <see cref="SizeClass"/>. This converter resolves both gaps deliberately: one <see cref="SceneModel.UiProfile"/>
/// is produced per platform - a platform is a deployment target of its own, which is what
/// <see cref="RenderPlanner"/> plans against (Cratis/Stage#39) - and a bare <c>target size</c> name is applied
/// to both the width and height axis (Screenplay has no per-axis default size syntax).
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
            new SceneModel.UiProfile(uiProfile.Name, platform, packages, defaultSizeClass, uiProfile.Layout, uiProfile.Theme));
    }

    static SizeClass? ConvertDefaultSizeClass(string? defaultSizeClass)
    {
        if (defaultSizeClass is null)
        {
            return null;
        }

        return new SizeClass(SizeClassNames.ParseWidth(defaultSizeClass), SizeClassNames.ParseHeight(defaultSizeClass));
    }
}
