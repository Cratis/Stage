// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneStructure = Cratis.Scene.Model.Structure;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.LayoutSyntax"/> - the application's base
/// navigational shell - into a <see cref="SceneLayouts.Layout"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A <c language="csharp">layout</c> is a top-level Screenplay declaration, alongside <c language="csharp">theme</c> and <c language="csharp">ui profile</c>, and an
/// application selects one of them per <c language="csharp">ui profile</c>. The reusable shapes that go <em>inside</em> the
/// shell - the ones that used to share the word "layout" - are screen and dialog templates now, converted by
/// <see cref="ScreenTemplateConverter"/> and <see cref="DialogTemplateConverter"/>. All three are structurally
/// alike (slots plus an arrangement) and differ only in role, which is exactly how both languages model them.
/// <para>
/// The layout's outlets carry over as Scene outlets (<see cref="OutletConverter"/>), except its outlets of navigation and the actions outlets
/// that hold their buttons: those are the application's navigation (<see cref="NavigationConverter"/>), which Scene's outlet chain puts
/// above the layout.
/// </para>
/// </remarks>
public static class LayoutConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.LayoutSyntax"/> into a <see cref="SceneLayouts.Layout"/>.
    /// </summary>
    /// <param name="layout">The <see cref="ScreenplaySyntax.LayoutSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneLayouts.Layout"/>.</returns>
    public static SceneLayouts.Layout Convert(ScreenplaySyntax.LayoutSyntax layout) => Convert(layout, null);

    /// <summary>
    /// Converts a layout while collecting arrangement omissions.
    /// </summary>
    /// <param name="layout">The layout to convert.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <returns>The converted layout.</returns>
    public static SceneLayouts.Layout Convert(ScreenplaySyntax.LayoutSyntax layout, ConversionDiagnostics? diagnostics) => Convert(layout, diagnostics, null);

    /// <summary>
    /// Converts a layout while collecting arrangement omissions, writing the slices its buttons name as the qualified ids the structure knows.
    /// </summary>
    /// <param name="layout">The layout to convert.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <param name="structure">The structure slices resolve against, or null to keep them as written.</param>
    /// <returns>The converted layout.</returns>
    public static SceneLayouts.Layout Convert(ScreenplaySyntax.LayoutSyntax layout, ConversionDiagnostics? diagnostics, SceneStructure.ApplicationStructure? structure) =>
        new(layout.Name, SlotConverter.Convert(layout.Slots, layout.RecursiveSlots), diagnostics is null
            ? ArrangementConverter.Convert(layout.Arrangement)
            : ArrangementConverter.Convert(layout.Arrangement, diagnostics, layout.Name))
        {
            Metadata = CompositionConverter.Metadata(layout.TemplateType, layout.Category, layout.RestrictsScopes, layout.Scopes, layout.SliceKinds, diagnostics, layout.Name, layout.Location),
            Outlets = Outlets(layout, structure)
        };

    static List<Cratis.Scene.Model.Outlets.Outlet>? Outlets(ScreenplaySyntax.LayoutSyntax layout, SceneStructure.ApplicationStructure? structure)
    {
        var navigation = layout.Outlets
            .Where(outlet => outlet.Kind == ScreenplaySyntax.OutletKind.Navigation)
            .Select(outlet => outlet.Name)
            .ToHashSet(StringComparer.Ordinal);
        var outlets = layout.Outlets
            .Where(outlet => OutletConverter.IsDeclaration(outlet) && !NavigationConverter.IsNavigation(outlet.Name, navigation))
            .Select(outlet => OutletConverter.Declaration(outlet, structure))
            .ToList();
        return outlets.Count > 0 ? outlets : null;
    }
}
