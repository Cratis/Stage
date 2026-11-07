// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Outlets;
using SceneOutlets = Cratis.Scene.Model.Outlets;
using SceneStructure = Cratis.Scene.Model.Structure;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts what the modules, features and slice screens of a compiled Screenplay application do to the outlets above them into the
/// outlet levels Scene's engine resolves an outlet through.
/// </summary>
/// <remarks>
/// <para>
/// Scene's document carries the outlets a layout or template declares, a screen's outlet contributions and the navigation model; the
/// levels between them - modules and features - are the host's (<see cref="OutletChainSource.Levels"/>). Each module and feature that
/// refines an outlet (<c language="csharp">outlet &lt;name&gt; inherits|extends|overrides</c>) or contributes items to one
/// (<c language="csharp">contribute to</c>) becomes an <see cref="OutletLevel"/> named as the screen's <c language="csharp">path</c> names it.
/// A module's refinements of and contributions to navigation outlets live on its navigation instead (<see cref="NavigationConverter"/>).
/// </para>
/// <para>
/// A slice screen's own refinements are a <see cref="OutletLevelKind.Slice"/> level named after the screen. Scene's chain builder does not
/// place that level, so <see cref="StageOutletChains"/> puts it between the screen's last feature and the screen.
/// </para>
/// </remarks>
public static class OutletLevelConverter
{
    /// <summary>
    /// Converts the outlet levels of an application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <param name="structure">The structure slices resolve against, if any.</param>
    /// <returns>The <see cref="OutletLevel"/> of every module, feature and slice screen that changes an outlet.</returns>
    public static IReadOnlyList<OutletLevel> Convert(ScreenplaySyntax.ApplicationSyntax application, SceneStructure.ApplicationStructure? structure)
    {
        var navigationOutlets = NavigationConverter.NavigationOutletNames(application);
        var levels = new List<OutletLevel>();
        foreach (var module in application.Modules)
        {
            Add(
                levels,
                OutletLevelKind.Module,
                module.Name,
                module.Outlets.Where(outlet => !NavigationConverter.IsNavigation(outlet.Name, navigationOutlets)),
                (module.Contributions ?? []).Where(contribution => !NavigationConverter.IsNavigation(contribution.ContributionPoint, navigationOutlets)),
                navigationOutlets,
                structure);
            foreach (var feature in module.Features) AddFeature(levels, feature, navigationOutlets, structure);
        }

        return levels;
    }

    static void AddFeature(List<OutletLevel> levels, ScreenplaySyntax.FeatureSyntax feature, IReadOnlySet<string> navigationOutlets, SceneStructure.ApplicationStructure? structure)
    {
        Add(levels, OutletLevelKind.Feature, feature.Name, feature.Outlets, feature.Contributions ?? [], navigationOutlets, structure);
        foreach (var screen in feature.Slices.SelectMany(slice => slice.Screens))
        {
            Add(levels, OutletLevelKind.Slice, screen.Name, screen.Outlets, [], navigationOutlets, structure);
        }

        foreach (var subFeature in feature.Features) AddFeature(levels, subFeature, navigationOutlets, structure);
    }

    static void Add(
        List<OutletLevel> levels,
        OutletLevelKind kind,
        string name,
        IEnumerable<ScreenplaySyntax.OutletSyntax> outlets,
        IEnumerable<ScreenplaySyntax.ContributionSyntax> contributions,
        IReadOnlySet<string> navigationOutlets,
        SceneStructure.ApplicationStructure? structure)
    {
        var refinements = outlets
            .Where(outlet => !OutletConverter.IsDeclaration(outlet))
            .Select(outlet => OutletConverter.Refinement(outlet, structure, OutletConverter.SceneName(outlet.Name, navigationOutlets)))
            .ToList();
        var contributed = contributions
            .Select(contribution => OutletConverter.Contribution(contribution, structure, OutletConverter.SceneName(contribution.ContributionPoint, navigationOutlets)))
            .OfType<SceneOutlets.OutletContribution>()
            .ToList();
        if (refinements.Count == 0 && contributed.Count == 0) return;

        levels.Add(new(kind, name, Refinements: refinements.Count > 0 ? refinements : null, Contributions: contributed.Count > 0 ? contributed : null));
    }
}
