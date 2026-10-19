// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneNavigation = Cratis.Scene.Model.Navigation;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts the <c language="csharp">navigation topbar</c> and <c language="csharp">navigation sidebar</c> blocks of a compiled Screenplay application into
/// Scene's <see cref="SceneNavigation.NavigationModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// The application's regions and logo come from the first declared <c language="csharp">layout</c> - the shell every screen is translated
/// against, see <see cref="ScreenplaySceneVisitor"/>. A module contributes a <see cref="SceneNavigation.ModuleNavigation"/> only when it
/// declares a block; a region it does not declare is <see cref="SceneNavigation.NavigationRegionMode.Inherit"/> in Scene, which the
/// engine's <c language="csharp">EffectiveNavigation</c> resolves, so nothing here computes effective items.
/// </para>
/// <para>
/// An item's target is a <see cref="SceneCommon.DestinationReference"/> naming the module and, when written, the dotted feature path. A grouping
/// item with no target keeps none. Titles stay as written, a <c language="csharp">$strings.&lt;key&gt;</c> token included, as every other label does.
/// </para>
/// </remarks>
public static class NavigationConverter
{
    /// <summary>
    /// Converts the navigation of an application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The <see cref="SceneNavigation.NavigationModel"/>, empty when the document declares no region.</returns>
    public static SceneNavigation.NavigationModel Convert(ScreenplaySyntax.ApplicationSyntax application) => Convert(application, null);

    /// <summary>
    /// Converts the navigation of a compiled application and reports what Scene's navigation model cannot carry.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <param name="diagnostics">The collector for omissions, or null to omit them silently as the throwing converter always did.</param>
    /// <returns>The <see cref="SceneNavigation.NavigationModel"/>, empty when the document declares no region.</returns>
    public static SceneNavigation.NavigationModel Convert(ScreenplaySyntax.ApplicationSyntax application, ConversionDiagnostics? diagnostics)
    {
        ReportButtons(application, diagnostics);
        var layout = application.Layouts?.FirstOrDefault();
        var regions = layout?.Navigation.ToList() ?? [];
        var logo = regions.Select(region => region.Logo).FirstOrDefault(candidate => candidate is not null);

        return new SceneNavigation.NavigationModel(
            new SceneNavigation.ApplicationNavigation(
                [.. regions.Select(region => Convert(region, applicationLevel: true))],
                logo is null ? null : new SceneNavigation.ApplicationLogo(logo.Image, logo.Title)),
            [.. application.Modules
                .Where(module => module.Navigation.Any())
                .Select(module => new SceneNavigation.ModuleNavigation(module.Name, [.. module.Navigation.Select(region => Convert(region, applicationLevel: false))]))]);
    }

    // Application-level toolbar buttons (Screenplay ESM-free source construct, decision 0098): Scene's NavigationRegion carries items only, so
    // a button in a navigation topbar cannot reach a running Stage today. Say so instead of dropping it unseen.
    static void ReportButtons(ScreenplaySyntax.ApplicationSyntax application, ConversionDiagnostics? diagnostics)
    {
        if (diagnostics is null)
        {
            return;
        }

        var regions = (application.Layouts ?? []).SelectMany(layout => layout.Navigation)
            .Concat(application.Modules.SelectMany(module => module.Navigation));
        foreach (var button in regions.SelectMany(region => region.Buttons))
        {
            diagnostics.Report(new(
                "STAGE-SCENE-NAV-001",
                Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error,
                "application toolbar button",
                button.Slice ?? button.Id,
                button.Location,
                $"Application toolbar button '{button.Id}' was omitted: Scene's navigation regions carry items only, so a button in 'navigation topbar' cannot be rendered yet."));
        }
    }

    static SceneNavigation.NavigationRegion Convert(ScreenplaySyntax.NavigationRegionSyntax region, bool applicationLevel) =>
        new(
            region.Region == ScreenplaySyntax.NavigationRegionKind.Sidebar ? SceneNavigation.NavigationRegionKind.Sidebar : SceneNavigation.NavigationRegionKind.Topbar,
            [.. region.Items.Select(Convert)],
            applicationLevel ? null : ModeOf(region.Mode));

    static SceneNavigation.NavigationRegionMode? ModeOf(ScreenplaySyntax.NavigationRegionMode mode) => mode switch
    {
        ScreenplaySyntax.NavigationRegionMode.Extend => SceneNavigation.NavigationRegionMode.Extend,
        ScreenplaySyntax.NavigationRegionMode.Override => SceneNavigation.NavigationRegionMode.Override,
        _ => SceneNavigation.NavigationRegionMode.Inherit
    };

    static SceneNavigation.NavItem Convert(ScreenplaySyntax.NavigationItemSyntax item) =>
        new(
            item.Id,
            item.Title,
            item.Icon,
            item.Target is null ? null : new SceneCommon.DestinationReference(Module: item.Target.Module, Feature: item.Target.Feature),
            item.Slot,
            item.Children.Any() ? [.. item.Children.Select(Convert)] : null);
}
