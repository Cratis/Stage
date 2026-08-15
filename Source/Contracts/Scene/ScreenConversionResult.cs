// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The result of converting a Screenplay <c>ScreenSyntax</c> - see <see cref="ScreenConverter"/>.
/// </summary>
/// <param name="Screen">The converted <see cref="SceneScreens.Screen"/>.</param>
/// <param name="ImplicitLayout">The synthesized <see cref="SceneLayouts.Layout"/> for a screen with no explicit <c>layout</c> directive, or <see langword="null"/> when the screen references a real one.</param>
public record ScreenConversionResult(SceneScreens.Screen Screen, SceneLayouts.Layout? ImplicitLayout);
