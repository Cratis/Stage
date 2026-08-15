// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneProfiles = Cratis.Scene.Model.Profiles;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The full result of translating a compiled Screenplay <c>ApplicationSyntax</c> into Scene - part of
/// Cratis/Stage#37. A Stage-side bundle, not a Scene concept - <c>Cratis.Scene.Model</c> has no "whole
/// translated app" type, and none should exist there.
/// </summary>
/// <param name="UiProfiles">Every <see cref="SceneProfiles.UiProfile"/> declared in the application.</param>
/// <param name="Themes">Every <see cref="SceneProfiles.Theme"/> declared in the application.</param>
/// <param name="Layouts">Every <see cref="SceneLayouts.Layout"/>, both explicitly declared and implicitly synthesized for Level-1/file-referenced screens.</param>
/// <param name="Screens">Every <see cref="SceneScreens.Screen"/> across every slice.</param>
public record SceneApplication(
    IReadOnlyList<SceneProfiles.UiProfile> UiProfiles,
    IReadOnlyList<SceneProfiles.Theme> Themes,
    IReadOnlyList<SceneLayouts.Layout> Layouts,
    IReadOnlyList<SceneScreens.Screen> Screens);
