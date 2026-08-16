// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The single-slot application shell used when a Screenplay document declares no <c>layout</c> of its own -
/// part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// <see cref="Cratis.Scene.Model.Screens.Screen.Layout"/> is a required, non-null name, but <c>layout</c> is
/// optional in Screenplay - a document can declare screens and never state a shell, and the shell is selected
/// by a <c>ui profile</c> rather than by a screen. One shell is synthesized for the whole application in that
/// case, never one per screen: an application has exactly one layout in force, so a shell per screen would
/// contradict the taxonomy the language and <c>Cratis.Scene.Model</c> both implement.
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
    /// <c>fits slot content</c> every module-level screen template declares against it.
    /// </summary>
    public const string ContentSlotName = "content";

    /// <summary>
    /// Creates the synthesized shell.
    /// </summary>
    /// <returns>A <see cref="SceneLayouts.Layout"/> with a single <see cref="ContentSlotName"/> slot.</returns>
    public static SceneLayouts.Layout Create() => new(Name, [new SceneLayouts.Slot(ContentSlotName)]);
}
