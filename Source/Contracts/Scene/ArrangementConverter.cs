// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.ArrangementSyntax"/> into a
/// <see cref="SceneLayouts.Arrangement"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// <para>
/// <c>arrangement</c> works identically on all three slot-bearing Screenplay declarations - the application's
/// <c>layout</c>, a <c>screen template</c> and a <c>dialog template</c> - so all three converters share this
/// one conversion. It arranges the declaring structure's own named slots relative to each other, which is why
/// it produces the slot-referencing <see cref="SceneLayouts.FlowSlotLeaf"/>/<see cref="SceneLayouts.SlotPlacement"/>
/// leaves rather than element leaves.
/// </para>
/// <para>
/// A <c>place</c> marked <c>hidden</c> is excluded from its variant's placements entirely -
/// <see cref="SceneLayouts.SlotPlacement"/> has no hidden flag, so "hidden in this variant" is represented as
/// "absent from this variant". A flow slot leaf's fixed <c>width</c>/<c>height</c> is a known, deliberate gap:
/// <see cref="SceneLayouts.FlowNode"/> carries only <c>Grow</c> and <c>Span</c>, so a fixed size on a leaf is
/// not carried through.
/// </para>
/// </remarks>
public static class ArrangementConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ArrangementSyntax"/> into a <see cref="SceneLayouts.Arrangement"/>.
    /// </summary>
    /// <param name="arrangement">The <see cref="ScreenplaySyntax.ArrangementSyntax"/> to convert, or <see langword="null"/> when the structure declares none.</param>
    /// <returns>The converted <see cref="SceneLayouts.Arrangement"/>, or <see langword="null"/> when there is nothing to arrange.</returns>
    public static SceneLayouts.Arrangement? Convert(ScreenplaySyntax.ArrangementSyntax? arrangement) =>
        arrangement switch
        {
            { Mode: ScreenplaySyntax.ArrangementMode.Flow, Root: not null } => ConvertFlow(arrangement),
            { Mode: ScreenplaySyntax.ArrangementMode.Freeform, Variants: not null } => ConvertFreeform(arrangement.Variants),
            _ => null,
        };

    static SceneLayouts.FlowArrangement ConvertFlow(ScreenplaySyntax.ArrangementSyntax arrangement) =>
        new(ConvertNode(arrangement.Root!), [.. (arrangement.Overrides ?? []).Select(ConvertOverride)]);

    static SceneLayouts.FlowOverride ConvertOverride(ScreenplaySyntax.ArrangementOverrideSyntax @override) =>
        new(
            @override.Width is null ? null : SizeClassNames.ParseWidth(@override.Width),
            @override.Height is null ? null : SizeClassNames.ParseHeight(@override.Height),
            ConvertNode(@override.Root));

    static SceneLayouts.FlowNode ConvertNode(ScreenplaySyntax.ArrangementNodeSyntax node) =>
        node switch
        {
            ScreenplaySyntax.ArrangementSlotSyntax slot => new SceneLayouts.FlowSlotLeaf(slot.Name) { Grow = slot.Grow ? 1 : null, Span = slot.Span },
            ScreenplaySyntax.ArrangementContainerSyntax container => ConvertContainer(container),
            _ => throw new UnknownArrangementNode(node.GetType().Name),
        };

    static SceneLayouts.FlowContainer ConvertContainer(ScreenplaySyntax.ArrangementContainerSyntax container)
    {
        var children = container.Children.Select(ConvertNode).ToList();
        var gap = container.Gap ?? 0;

        // Flat (no explicit row/column/grid nesting) has no Scene.Model counterpart - stacking vertically
        // (a column) is the closest match to how an unordered group of slots typically reads on a page.
        return container.Kind switch
        {
            ScreenplaySyntax.ArrangementContainerKind.Row => new SceneLayouts.FlowRow { Gap = gap, Children = children },
            ScreenplaySyntax.ArrangementContainerKind.Column => new SceneLayouts.FlowColumn { Gap = gap, Children = children },
            ScreenplaySyntax.ArrangementContainerKind.Grid => new SceneLayouts.FlowGrid { Gap = gap, Children = children },
            ScreenplaySyntax.ArrangementContainerKind.Flat => new SceneLayouts.FlowColumn { Gap = gap, Children = children },
            _ => throw new UnknownArrangementContainerKind(container.Kind),
        };
    }

    static SceneLayouts.FreeformSlotArrangement ConvertFreeform(IEnumerable<ScreenplaySyntax.VariantSyntax> variants) =>
        new([.. variants.Select(ConvertVariant)]);

    static SceneLayouts.FreeformSlotVariant ConvertVariant(ScreenplaySyntax.VariantSyntax variant) =>
        new(
            new(SizeClassNames.ParseWidth(variant.Width), SizeClassNames.ParseHeight(variant.Height)),
            [.. variant.Places.Where(place => !place.Hidden).Select(ConvertPlacement)]);

    static SceneLayouts.SlotPlacement ConvertPlacement(ScreenplaySyntax.PlaceSyntax place) =>
        new(place.SlotName, place.X ?? 0, place.Y ?? 0, ParseSize(place.SizeWidth), ParseSize(place.SizeHeight));

    static double ParseSize(string? size) => size is null || size == "fill" ? 0 : double.Parse(size);
}
