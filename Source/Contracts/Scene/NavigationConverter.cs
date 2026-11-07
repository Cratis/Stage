// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneNavigation = Cratis.Scene.Model.Navigation;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneStructure = Cratis.Scene.Model.Structure;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts the navigation outlets of a compiled Screenplay application into Scene's <see cref="SceneNavigation.NavigationModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// The application's navigation outlets and logo come from the first declared <c language="csharp">layout</c> - the shell every screen is
/// translated against, see <see cref="ScreenplaySceneVisitor"/>. Each <c language="csharp">outlet &lt;name&gt; of navigation</c> becomes an
/// application outlet of navigation, and the layout's actions outlet that holds its buttons (<c language="csharp">topbarActions</c>) becomes
/// the outlet Scene draws beside it (<c language="csharp">topbarButtons</c>).
/// </para>
/// <para>
/// A module's <c language="csharp">outlet &lt;name&gt; inherits|extends|overrides</c> of one of those outlets is a
/// <see cref="SceneOutlets.OutletRefinement"/> of its <see cref="SceneNavigation.ModuleNavigation"/>, and a module's
/// <c language="csharp">contribute to</c> with items is one of its <see cref="SceneOutlets.OutletContribution"/>. Outlets that are not
/// navigation belong to the module's outlet level (<see cref="OutletLevelConverter"/>). Nothing here computes effective items: Scene's engine
/// resolves them (<c language="csharp">EffectiveNavigation</c>, <c language="csharp">EffectiveOutlets</c>).
/// </para>
/// </remarks>
public static class NavigationConverter
{
    /// <summary>
    /// Converts the navigation of an application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The <see cref="SceneNavigation.NavigationModel"/>, empty when the document declares no navigation outlet.</returns>
    public static SceneNavigation.NavigationModel Convert(ScreenplaySyntax.ApplicationSyntax application) => Convert(application, null);

    /// <summary>
    /// Converts the navigation of a compiled application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <param name="diagnostics">The collector for omissions, or null to omit them silently.</param>
    /// <returns>The <see cref="SceneNavigation.NavigationModel"/>, empty when the document declares no navigation outlet.</returns>
    public static SceneNavigation.NavigationModel Convert(ScreenplaySyntax.ApplicationSyntax application, ConversionDiagnostics? diagnostics) =>
        Convert(application, diagnostics, null);

    /// <summary>
    /// Converts the navigation of a compiled application, writing each button's slice as the qualified id the structure knows.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <param name="diagnostics">The collector for omissions, or null to omit them silently.</param>
    /// <param name="structure">The structure a button's slice reference is resolved against, or null to keep it as written.</param>
    /// <returns>The <see cref="SceneNavigation.NavigationModel"/>, empty when the document declares no navigation outlet.</returns>
    public static SceneNavigation.NavigationModel Convert(ScreenplaySyntax.ApplicationSyntax application, ConversionDiagnostics? diagnostics, SceneStructure.ApplicationStructure? structure)
    {
        var layout = application.Layouts?.FirstOrDefault();
        var navigationOutlets = NavigationOutletNames(application);
        var declared = (layout?.Outlets ?? [])
            .Where(outlet => OutletConverter.IsDeclaration(outlet) && IsNavigation(outlet.Name, navigationOutlets))
            .ToList();
        var logo = declared.Select(outlet => outlet.Logo).FirstOrDefault(candidate => candidate is not null);

        return new SceneNavigation.NavigationModel(
            new SceneNavigation.ApplicationNavigation(
                [.. declared.Select(outlet => OutletConverter.Declaration(outlet, structure, OutletConverter.SceneName(outlet.Name, navigationOutlets)))],
                logo is null ? null : new SceneNavigation.ApplicationLogo(logo.Image, logo.Title)),
            [.. application.Modules
                .Select(module => Convert(module, navigationOutlets, structure))
                .OfType<SceneNavigation.ModuleNavigation>()]);
    }

    /// <summary>
    /// Gives the names of the navigation outlets the application's layout declares.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The authored names of its outlets of navigation.</returns>
    public static IReadOnlySet<string> NavigationOutletNames(ScreenplaySyntax.ApplicationSyntax application) =>
        (application.Layouts?.FirstOrDefault()?.Outlets ?? [])
            .Where(outlet => outlet.Kind == ScreenplaySyntax.OutletKind.Navigation)
            .Select(outlet => outlet.Name)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Tells whether an authored outlet belongs to the navigation model: a navigation outlet, or the actions outlet that holds its buttons.
    /// </summary>
    /// <param name="name">The authored outlet name.</param>
    /// <param name="navigationOutlets">The names of the application's navigation outlets.</param>
    /// <returns>Whether the outlet is navigation.</returns>
    public static bool IsNavigation(string name, IReadOnlySet<string> navigationOutlets) =>
        navigationOutlets.Contains(name) || OutletConverter.SceneName(name, navigationOutlets) != name;

    static SceneNavigation.ModuleNavigation? Convert(ScreenplaySyntax.ModuleSyntax module, IReadOnlySet<string> navigationOutlets, SceneStructure.ApplicationStructure? structure)
    {
        var refinements = module.Outlets
            .Where(outlet => !OutletConverter.IsDeclaration(outlet) && IsNavigation(outlet.Name, navigationOutlets))
            .Select(outlet => OutletConverter.Refinement(outlet, structure, OutletConverter.SceneName(outlet.Name, navigationOutlets)))
            .ToList();
        var contributions = (module.Contributions ?? [])
            .Where(contribution => IsNavigation(contribution.ContributionPoint, navigationOutlets))
            .Select(contribution => OutletConverter.Contribution(contribution, structure, OutletConverter.SceneName(contribution.ContributionPoint, navigationOutlets)))
            .OfType<SceneOutlets.OutletContribution>()
            .ToList();

        return refinements.Count == 0 && contributions.Count == 0
            ? null
            : new SceneNavigation.ModuleNavigation(module.Name, refinements.Count > 0 ? refinements : null, contributions.Count > 0 ? contributions : null);
    }
}
