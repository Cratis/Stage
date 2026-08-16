// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts the slots a Screenplay <c>layout</c>, <c>screen template</c> or <c>dialog template</c> declares
/// into <see cref="SceneLayouts.Slot"/>s - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A slot is declared once, by name, in the declaring structure's body, and the <c>arrangement</c> then only
/// positions it - so a converted <see cref="SceneLayouts.Slot"/> always leaves its own <c>Arrangement</c>
/// <see langword="null"/>: Screenplay has no syntax for arranging content <em>within</em> one slot, and the
/// structure's own arrangement belongs on the structure (see <see cref="ArrangementConverter"/>), not on each
/// slot. <see cref="ScreenplaySyntax.SlotSyntax.Contributes"/> - the contribution point a slot opens itself up
/// to - has no home on <see cref="SceneLayouts.Slot"/> and is a known, deliberate gap: not carried through.
/// </remarks>
public static class SlotConverter
{
    /// <summary>
    /// Converts the declared <see cref="ScreenplaySyntax.SlotSyntax"/> into <see cref="SceneLayouts.Slot"/>s.
    /// </summary>
    /// <param name="slots">The <see cref="ScreenplaySyntax.SlotSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneLayouts.Slot"/>, in declaration order.</returns>
    public static IReadOnlyList<SceneLayouts.Slot> Convert(IEnumerable<ScreenplaySyntax.SlotSyntax>? slots) =>
        [.. (slots ?? []).Select(slot => new SceneLayouts.Slot(slot.Name))];
}
