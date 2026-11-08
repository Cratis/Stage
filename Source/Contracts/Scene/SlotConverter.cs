// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts the slots a Screenplay <c language="csharp">layout</c>, <c language="csharp">screen template</c> or <c language="csharp">dialog template</c> declares
/// into <see cref="SceneLayouts.Slot"/>s - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// <para>
/// A slot is declared once, by name, in the declaring structure's body, and the <c language="csharp">arrangement</c> then only
/// positions it - so a converted <see cref="SceneLayouts.Slot"/> always leaves its own <c language="csharp">Arrangement</c>
/// <see langword="null"/>: Screenplay has no syntax for arranging content <em>within</em> one slot, and the
/// structure's own arrangement belongs on the structure (see <see cref="ArrangementConverter"/>), not on each slot.
/// </para>
/// <para>
/// What a slot is for carries over as written: its role (<c language="csharp">page</c>, <c language="csharp">pane</c> or
/// <c language="csharp">panel</c>), <c language="csharp">routed</c> and the parameter it is <c language="csharp">routed by</c>,
/// <c language="csharp">primary</c> and <c language="csharp">optional</c>. A word that is not written stays unstated, so Scene resolves a
/// slot without a role itself (<c language="csharp">SlotRoles.Resolve</c>).
/// </para>
/// <para>
/// A recursive fill point (<c language="csharp">slot &lt;name&gt;</c>, <see cref="ScreenplaySyntax.TemplateSlotSyntax"/>) is a routed slot, the
/// shape Scene's document upgrader gives the nested outlets Scene 4 stored.
/// </para>
/// </remarks>
public static class SlotConverter
{
    /// <summary>
    /// Converts the declared <see cref="ScreenplaySyntax.SlotSyntax"/> into <see cref="SceneLayouts.Slot"/>s.
    /// </summary>
    /// <param name="slots">The <see cref="ScreenplaySyntax.SlotSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneLayouts.Slot"/>, in declaration order.</returns>
    public static IReadOnlyList<SceneLayouts.Slot> Convert(IEnumerable<ScreenplaySyntax.SlotSyntax>? slots) =>
        Convert(slots, null);

    /// <summary>
    /// Converts the declared <see cref="ScreenplaySyntax.SlotSyntax"/> and the recursive fill points into <see cref="SceneLayouts.Slot"/>s.
    /// </summary>
    /// <param name="slots">The <see cref="ScreenplaySyntax.SlotSyntax"/> to convert.</param>
    /// <param name="recursiveSlots">The <see cref="ScreenplaySyntax.TemplateSlotSyntax"/> fill points, if any.</param>
    /// <returns>The converted <see cref="SceneLayouts.Slot"/>, the declared ones in declaration order, then the fill points they do not name.</returns>
    public static IReadOnlyList<SceneLayouts.Slot> Convert(IEnumerable<ScreenplaySyntax.SlotSyntax>? slots, IEnumerable<ScreenplaySyntax.TemplateSlotSyntax>? recursiveSlots)
    {
        var converted = (slots ?? []).Select(Convert).ToList();
        var names = converted.Select(slot => slot.Name).ToHashSet(StringComparer.Ordinal);
        converted.AddRange((recursiveSlots ?? [])
            .Where(slot => names.Add(slot.Name))
            .Select(slot => new SceneLayouts.Slot(slot.Name) { Routed = true }));
        return converted;
    }

    /// <summary>
    /// Converts one declared slot.
    /// </summary>
    /// <param name="slot">The <see cref="ScreenplaySyntax.SlotSyntax"/>.</param>
    /// <returns>The <see cref="SceneLayouts.Slot"/>.</returns>
    public static SceneLayouts.Slot Convert(ScreenplaySyntax.SlotSyntax slot) =>
        new(slot.Name)
        {
            Role = slot.Role switch
            {
                ScreenplaySyntax.SlotRole.Page => SceneLayouts.SlotRole.Page,
                ScreenplaySyntax.SlotRole.Pane => SceneLayouts.SlotRole.Pane,
                ScreenplaySyntax.SlotRole.Panel => SceneLayouts.SlotRole.Panel,
                _ => null
            },
            Routed = slot.Routed || slot.RoutedBy is not null ? true : null,
            RoutedBy = slot.RoutedBy is { } parameter ? [parameter] : null,
            Primary = slot.Primary ? true : null,
            Optional = slot.Optional ? true : null
        };
}
