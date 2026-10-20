// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The legacy single-slot layout factory and the conventional content slot name.
/// </summary>
/// <remarks>
/// Retained for callers that explicitly construct this layout. Translation and synthesis leave layouts empty
/// when the model declares none, so Scene can select the blueprint's renderable default shell instead.
/// </remarks>
public static class DefaultLayout
{
    /// <summary>
    /// The name of the synthesized shell.
    /// </summary>
    public const string Name = "Application";

    /// <summary>
    /// The name of the slot a screen's content fills when the screen names no screen template - the
    /// conventional name of the content region, both in Screenplay's own documented shell and in the
    /// <c language="csharp">fits slot content</c> every module-level screen template declares against it.
    /// </summary>
    public const string ContentSlotName = "content";

    /// <summary>
    /// Creates the synthesized shell.
    /// </summary>
    /// <returns>A <see cref="SceneLayouts.Layout"/> with a single <see cref="ContentSlotName"/> slot.</returns>
    public static SceneLayouts.Layout Create() => new(Name, [new SceneLayouts.Slot(ContentSlotName)]);
}
