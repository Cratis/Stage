// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.LayoutSyntax"/> - the application's base
/// navigational shell - into a <see cref="SceneLayouts.Layout"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A <c>layout</c> is a top-level Screenplay declaration, alongside <c>theme</c> and <c>ui profile</c>, and an
/// application selects one of them per <c>ui profile</c>. The reusable shapes that go <em>inside</em> the
/// shell - the ones that used to share the word "layout" - are screen and dialog templates now, converted by
/// <see cref="ScreenTemplateConverter"/> and <see cref="DialogTemplateConverter"/>. All three are structurally
/// alike (slots plus an arrangement) and differ only in role, which is exactly how both languages model them.
/// </remarks>
public static class LayoutConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.LayoutSyntax"/> into a <see cref="SceneLayouts.Layout"/>.
    /// </summary>
    /// <param name="layout">The <see cref="ScreenplaySyntax.LayoutSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneLayouts.Layout"/>.</returns>
    public static SceneLayouts.Layout Convert(ScreenplaySyntax.LayoutSyntax layout) =>
        new(layout.Name, SlotConverter.Convert(layout.Slots), ArrangementConverter.Convert(layout.Arrangement));
}
