// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Layouts;
using Cratis.Scene.Engine.Packages;
using Cratis.Scene.Engine.Profiles;
using Cratis.Scene.Engine.Screens;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using ScenePackagesModel = Cratis.Scene.Model.Packages;
using SceneProfilesModel = Cratis.Scene.Model.Profiles;
using SceneSizeClasses = Cratis.Scene.Model.SizeClasses;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Resolves a translated <see cref="SceneApplication"/> into one <see cref="RenderPlan"/> per deployment
/// target - Cratis/Stage#39.
/// </summary>
/// <remarks>
/// <para>
/// Every resolution rule this needs already exists in <c>Cratis.Scene.Engine</c> and is used unmodified:
/// <see cref="PackageDependencyResolver"/> expands a target's packages, <see cref="PackageResolver"/> binds
/// component names against them, <see cref="ThemeCompatibility"/> checks the theme,
/// <see cref="ScreenTemplateResolver"/> nests the screen templates, and <see cref="SizeClassCalculator"/>
/// plus the three arrangement evaluators (through <see cref="ArrangementSelector"/>) decide size classes and
/// arrangements. Stage contributes the sequencing and the reporting, and deliberately no rules: a breakpoint,
/// priority or variant-matching rule written here would be a second answer to a question every Scene renderer
/// already answers, and the two would drift the moment either changed.
/// </para>
/// <para>
/// Nothing here throws for authored input. <see cref="EventModelLoader"/> has already thrown
/// <see cref="InvalidEventModel"/> if the <em>compilation</em> had errors, so this runs on a diagnostic-clean
/// syntax tree and everything it finds is a different class of problem: a <em>resolution</em> outcome, where
/// valid source turns out to be under-specified for one concrete target - a package the catalog never heard
/// of, a theme that does not cover a package, a component name nothing declares. Those belong on the returned
/// plan, per target, and are reported as <see cref="RenderFinding"/>s rather than pushed back into
/// Screenplay's diagnostic channel, which describes source text and has no target to attribute them to.
/// </para>
/// </remarks>
public static class RenderPlanner
{
    /// <summary>
    /// Plans every deployment target an application ships.
    /// </summary>
    /// <param name="application">The translated <see cref="SceneApplication"/> to plan.</param>
    /// <param name="catalog">Every package available to resolve against - the declarations behind the names a <c>ui profile</c> lists.</param>
    /// <returns>The <see cref="ApplicationRenderPlan"/>, carrying one <see cref="RenderPlan"/> per target.</returns>
    /// <remarks>
    /// There is deliberately no public entry point for planning a single target. Every target comes out of one
    /// compile of one source set, and planning them together is what makes them comparable and lets a build
    /// fail once with all of them in hand - see <see cref="ApplicationRenderPlan"/> for the full reasoning.
    /// Selecting which targets to emit is a filter over <see cref="ApplicationRenderPlan.Targets"/>, applied
    /// after planning rather than instead of it.
    /// </remarks>
    public static ApplicationRenderPlan Plan(SceneApplication application, IReadOnlyList<ScenePackagesModel.ScenePackage> catalog)
    {
        var targets = application.UiProfiles.Select(profile => PlanFor(profile, application, catalog)).ToList();
        return new ApplicationRenderPlan(targets, [.. RenderFindings.ForApplication(targets)]);
    }

    /// <summary>
    /// Resolves one target end to end.
    /// </summary>
    /// <param name="declared">The target as translated, with the packages it declares.</param>
    /// <param name="application">The application being planned.</param>
    /// <param name="catalog">Every package available to resolve against.</param>
    /// <returns>The target's <see cref="RenderPlan"/>.</returns>
    static RenderPlan PlanFor(
        SceneProfilesModel.UiProfile declared,
        SceneApplication application,
        IReadOnlyList<ScenePackagesModel.ScenePackage> catalog)
    {
        var target = Describe(declared);
        var selection = PackageDependencyResolver.Resolve(declared.Packages, catalog);

        // Everything below resolves against the expanded closure, in the priority order the resolver put it
        // in - that is the order Scene documents a profile's package list should carry once resolved.
        var profile = declared with { Packages = selection.Packages };
        var sizeClass = SizeClassFor(profile);
        var (layoutName, layout) = SelectLayout(profile, application, catalog);
        var theme = application.Themes.FirstOrDefault(candidate => candidate.Name == profile.Theme);

        var componentCatalog = PackageCatalog.ToComponentCatalog(catalog);
        var components = ComponentReferences.Collect(application)
            .Select(name => (Name: name, Resolution: PackageResolver.Resolve(name, profile, componentCatalog)))
            .ToList();

        // A shell whose structure is not in this model cannot place templates against its slots, and treating
        // it as an empty shell would report every module template as unplaceable, which is worse than silence.
        var screenTemplates = layout is null
            ? new ScreenTemplateResolution([], [], [])
            : ScreenTemplateResolver.Resolve(layout, application.ScreenTemplates);

        var arrangements = DeclaredArrangements(layout, application)
            .Select(declaredArrangement => (
                Name: Name(declaredArrangement.Structure, declaredArrangement.Slot),
                Selection: ArrangementSelector.Select(declaredArrangement.Structure, declaredArrangement.Slot, declaredArrangement.Arrangement, sizeClass)))
            .ToList();

        return new RenderPlan(
            profile,
            selection,
            layoutName,
            layout,
            theme,
            ThemePackages(theme, profile),
            sizeClass,
            [.. components.Where(component => component.Resolution is not null).Select(component => component.Resolution!)],
            screenTemplates,
            [.. arrangements.Where(arrangement => arrangement.Selection is not null).Select(arrangement => arrangement.Selection!)],
            [
                .. RenderFindings.ForPackages(profile, selection, catalog, target),
                .. RenderFindings.ForLayout(profile, application, layoutName, target),
                .. RenderFindings.ForTheme(profile, theme, target),
                .. RenderFindings.ForComponents(components.Where(component => component.Resolution is null).Select(component => component.Name), target),
                .. RenderFindings.ForScreenTemplates(screenTemplates, target),
                .. RenderFindings.ForSizeClass(arrangements.Where(arrangement => arrangement.Selection is null).Select(arrangement => arrangement.Name), sizeClass, target),
            ]);
    }

    /// <summary>
    /// Picks the size class a target's arrangements are evaluated at.
    /// </summary>
    /// <param name="profile">The target to pick for.</param>
    /// <returns>The target's declared default size class, or the one Scene's calculator yields at its own default breakpoints.</returns>
    /// <remarks>
    /// A build has no real dimensions to measure, so a target that declares no default size is planned at
    /// whatever <see cref="SizeClassCalculator"/> computes for exactly its default breakpoints. That keeps the
    /// number and the "at or above the breakpoint" rule in Scene rather than writing an assumed class here.
    /// </remarks>
    static SceneSizeClasses.SizeClass SizeClassFor(SceneProfilesModel.UiProfile profile) =>
        profile.DefaultSizeClass ??
        SizeClassCalculator.Compute(SizeClassCalculator.DefaultWidthBreakpoint, SizeClassCalculator.DefaultHeightBreakpoint);

    /// <summary>
    /// Scopes a theme's tokens to the packages they actually apply to.
    /// </summary>
    /// <param name="theme">The theme the target applies, or <see langword="null"/> when it applies none.</param>
    /// <param name="profile">The target, with its resolved package list.</param>
    /// <returns>The active packages the theme declares compatibility with, empty when there is no theme.</returns>
    static IReadOnlyList<string> ThemePackages(SceneProfilesModel.Theme? theme, SceneProfilesModel.UiProfile profile) =>
        theme is null ? [] : ThemeCompatibility.ApplicablePackages(theme, profile);

    /// <summary>
    /// Finds the shell a target renders inside.
    /// </summary>
    /// <param name="profile">The target, with its resolved package list.</param>
    /// <param name="application">The application being planned.</param>
    /// <param name="catalog">Every package available to resolve against.</param>
    /// <returns>The shell's name and, when the application declares it, its structure.</returns>
    /// <remarks>
    /// A target selecting no shell falls back to the application's first declared layout - the same one
    /// <see cref="ScreenplaySceneVisitor"/> resolved every screen against - so the common single-shell
    /// application needs no <c>layout</c> on its <c>ui profile</c> at all.
    /// </remarks>
    static (string? Name, SceneLayouts.Layout? Layout) SelectLayout(
        SceneProfilesModel.UiProfile profile,
        SceneApplication application,
        IReadOnlyList<ScenePackagesModel.ScenePackage> catalog)
    {
        if (profile.Layout is null)
        {
            var fallback = application.Layouts.Count > 0 ? application.Layouts[0] : null;
            return (fallback?.Name, fallback);
        }

        if (application.Layouts.FirstOrDefault(layout => layout.Name == profile.Layout) is { } selected)
        {
            return (selected.Name, selected);
        }

        var active = new HashSet<string>(profile.Packages, StringComparer.Ordinal);
        var provider = catalog.FirstOrDefault(package => active.Contains(package.Name) && package.Layouts.Contains(profile.Layout, StringComparer.Ordinal));

        return provider is null ? (null, null) : (profile.Layout, null);
    }

    /// <summary>
    /// Lists every arrangement a target renders, across its shell and the application's templates.
    /// </summary>
    /// <param name="layout">The target's shell, when its structure is known.</param>
    /// <param name="application">The application being planned.</param>
    /// <returns>Each declared arrangement, with the structure and slot it belongs to.</returns>
    static IEnumerable<(string Structure, string? Slot, SceneLayouts.Arrangement Arrangement)> DeclaredArrangements(
        SceneLayouts.Layout? layout,
        SceneApplication application)
    {
        var structures = application.ScreenTemplates
            .Select(template => (template.Name, template.Arrangement, template.Slots))
            .Concat(application.DialogTemplates.Select(template => (template.Name, template.Arrangement, template.Slots)));

        if (layout is not null)
        {
            structures = structures.Prepend((layout.Name, layout.Arrangement, layout.Slots));
        }

        return structures.SelectMany(structure => ArrangementsOf(structure.Name, structure.Arrangement, structure.Slots));
    }

    static IEnumerable<(string Structure, string? Slot, SceneLayouts.Arrangement Arrangement)> ArrangementsOf(
        string structure,
        SceneLayouts.Arrangement? own,
        IReadOnlyList<SceneLayouts.Slot> slots)
    {
        if (own is not null)
        {
            yield return (structure, null, own);
        }

        foreach (var slot in slots.Where(slot => slot.Arrangement is not null))
        {
            yield return (structure, slot.Name, slot.Arrangement!);
        }
    }

    static string Describe(SceneProfilesModel.UiProfile profile) => $"{profile.Name} ({profile.TargetPlatform})";

    static string Name(string structure, string? slot) => slot is null ? structure : $"{structure}.{slot}";
}
