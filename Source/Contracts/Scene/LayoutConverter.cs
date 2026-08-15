// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.LayoutSyntax"/> into a
/// <see cref="SceneLayouts.Layout"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// Screenplay's <c>template</c>/<c>variant</c> blocks arrange the layout's own named slots relative to each
/// other - they convert into <see cref="SceneLayouts.Layout.Arrangement"/> (using the slot-referencing
/// <see cref="SceneLayouts.FlowSlotLeaf"/>/<see cref="SceneLayouts.SlotPlacement"/> leaves added in
/// Cratis/Scene#14), never into a <see cref="SceneLayouts.Slot"/>'s own <c>Arrangement</c> - Screenplay has
/// no syntax yet for arranging content *within* one slot, so every converted <see cref="SceneLayouts.Slot"/>
/// leaves its own <c>Arrangement</c> <see langword="null"/>. A <c>place</c> marked <c>hidden</c> is excluded
/// from its variant's placements entirely - <see cref="SceneLayouts.SlotPlacement"/> has no hidden flag, so
/// "hidden in this variant" is represented as "absent from this variant".
/// </remarks>
public static class LayoutConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.LayoutSyntax"/> into a <see cref="SceneLayouts.Layout"/>.
    /// </summary>
    /// <param name="layout">The <see cref="ScreenplaySyntax.LayoutSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneLayouts.Layout"/>.</returns>
    public static SceneLayouts.Layout Convert(ScreenplaySyntax.LayoutSyntax layout)
    {
        var slots = layout.Slots.Select(slot => new SceneLayouts.Slot(slot.Name)).ToList();
        var arrangement = ConvertArrangement(layout);

        return new SceneLayouts.Layout(layout.Name, slots, arrangement);
    }

    static SceneLayouts.Arrangement? ConvertArrangement(ScreenplaySyntax.LayoutSyntax layout) =>
        layout.Arrangement switch
        {
            ScreenplaySyntax.LayoutArrangement.Flow when layout.Template is not null => ConvertFlowArrangement(layout.Template),
            ScreenplaySyntax.LayoutArrangement.Freeform when layout.Variants is not null => ConvertFreeformArrangement(layout.Variants),
            _ => null,
        };

    static SceneLayouts.FlowArrangement ConvertFlowArrangement(ScreenplaySyntax.TemplateSyntax template) =>
        new(ConvertFlowNode(template.Root), [.. template.Overrides.Select(ConvertFlowOverride)]);

    static SceneLayouts.FlowOverride ConvertFlowOverride(ScreenplaySyntax.TemplateOverrideSyntax @override) =>
        new(
            @override.Width is null ? null : SizeClassNames.ParseWidth(@override.Width),
            @override.Height is null ? null : SizeClassNames.ParseHeight(@override.Height),
            ConvertFlowNode(@override.Root));

    static SceneLayouts.FlowNode ConvertFlowNode(ScreenplaySyntax.TemplateNodeSyntax node) =>
        node switch
        {
            ScreenplaySyntax.TemplateSlotSyntax slot => new SceneLayouts.FlowSlotLeaf(slot.Name) { Grow = slot.Grow ? 1 : null, Span = slot.Span },
            ScreenplaySyntax.TemplateContainerSyntax container => ConvertFlowContainer(container),
            _ => throw new UnknownTemplateNode(node.GetType().Name),
        };

    static SceneLayouts.FlowContainer ConvertFlowContainer(ScreenplaySyntax.TemplateContainerSyntax container)
    {
        var children = container.Children.Select(ConvertFlowNode).ToList();
        var gap = container.Gap ?? 0;

        // Flat (no explicit row/column/grid nesting) has no Scene.Model counterpart - stacking vertically
        // (a column) is the closest match to how an unordered group of slots typically reads on a page.
        return container.Kind switch
        {
            ScreenplaySyntax.TemplateContainerKind.Row => new SceneLayouts.FlowRow { Gap = gap, Children = children },
            ScreenplaySyntax.TemplateContainerKind.Column => new SceneLayouts.FlowColumn { Gap = gap, Children = children },
            ScreenplaySyntax.TemplateContainerKind.Grid => new SceneLayouts.FlowGrid { Gap = gap, Children = children },
            ScreenplaySyntax.TemplateContainerKind.Flat => new SceneLayouts.FlowColumn { Gap = gap, Children = children },
            _ => throw new UnknownTemplateContainerKind(container.Kind),
        };
    }

    static SceneLayouts.FreeformSlotArrangement ConvertFreeformArrangement(IEnumerable<ScreenplaySyntax.VariantSyntax> variants) =>
        new([.. variants.Select(ConvertFreeformSlotVariant)]);

    static SceneLayouts.FreeformSlotVariant ConvertFreeformSlotVariant(ScreenplaySyntax.VariantSyntax variant) =>
        new(
            new(SizeClassNames.ParseWidth(variant.Width), SizeClassNames.ParseHeight(variant.Height)),
            [.. variant.Places.Where(place => !place.Hidden).Select(ConvertSlotPlacement)]);

    static SceneLayouts.SlotPlacement ConvertSlotPlacement(ScreenplaySyntax.PlaceSyntax place) =>
        new(place.SlotName, place.X ?? 0, place.Y ?? 0, ParseSize(place.SizeWidth), ParseSize(place.SizeHeight));

    static double ParseSize(string? size) => size is null || size == "fill" ? 0 : double.Parse(size);
}
