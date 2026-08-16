// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneModel = Cratis.Scene.Model.Profiles;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.ThemeSyntax"/> into a <see cref="SceneModel.Theme"/> -
/// part of Cratis/Stage#37, the translation seam between Screenplay and Scene.
/// </summary>
public static class ThemeConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ThemeSyntax"/> into a <see cref="SceneModel.Theme"/>.
    /// </summary>
    /// <param name="theme">The <see cref="ScreenplaySyntax.ThemeSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.Theme"/>.</returns>
    public static SceneModel.Theme Convert(ScreenplaySyntax.ThemeSyntax theme) => new(theme.Name, [.. theme.CompatibleWith]);
}
