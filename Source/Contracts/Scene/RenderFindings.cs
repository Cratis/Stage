// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Scene.Engine.Profiles;
using Cratis.Scene.Engine.Screens;
using ScenePackagesModel = Cratis.Scene.Model.Packages;
using SceneProfilesModel = Cratis.Scene.Model.Profiles;
using SceneSizeClasses = Cratis.Scene.Model.SizeClasses;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// States what a deployment target could not resolve - part of Cratis/Stage#39. <see cref="RenderPlanner"/>
/// runs Scene's engines; this turns what they returned into <see cref="RenderFinding"/>s.
/// </summary>
/// <remarks>
/// Kept apart from the planner because the two change for different reasons: the planner changes when the
/// resolution sequence does, this changes when the wording or the set of reported problems does. Everything
/// here reads an engine result and describes it - no rule is decided a second time.
/// </remarks>
public static class RenderFindings
{
    /// <summary>
    /// States what is wrong with an application independently of any one target.
    /// </summary>
    /// <param name="targets">The targets that were planned.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForApplication(IReadOnlyList<RenderPlan> targets) =>
        targets.Count > 0
            ? []
            : [new RenderFinding(
                RenderFindingKind.NoTargetDeclared,
                string.Empty,
                "The application declares no ui profile, so there is no deployment target to render for.")];

    /// <summary>
    /// States what a target's package list could not settle.
    /// </summary>
    /// <param name="profile">The target, with its resolved package list.</param>
    /// <param name="selection">The outcome of expanding its declared packages.</param>
    /// <param name="catalog">Every package available to resolve against.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForPackages(
        SceneProfilesModel.UiProfile profile,
        PackageSelection selection,
        IReadOnlyList<ScenePackagesModel.ScenePackage> catalog,
        string target)
    {
        var known = new HashSet<string>(catalog.Select(package => package.Name), StringComparer.Ordinal);

        // EffectivePackages, not the profile's own list: core is always active as the final fallback, so a
        // catalog without it cannot resolve the core vocabulary every translated screen references.
        return PackageResolver.EffectivePackages(profile)
            .Where(name => !known.Contains(name))
            .Select(name => new RenderFinding(
                RenderFindingKind.PackageNotInCatalog,
                name,
                $"Target '{target}' activates package '{name}', which the catalog does not declare."))
            .Concat(selection.Missing.Select(missing => new RenderFinding(
                RenderFindingKind.PackageDependencyMissing,
                missing.DependsOn,
                $"Package '{missing.Package}' depends on '{missing.DependsOn}', which nothing in the catalog satisfies for target '{target}'.")))
            .Concat(selection.VersionConflicts.Select(conflict => new RenderFinding(
                RenderFindingKind.PackageVersionConflict,
                conflict.DependsOn,
                $"Package '{conflict.Package}' needs '{conflict.DependsOn}' at '{conflict.RequiredRange}', but the catalog offers '{conflict.ActualVersion}' for target '{target}'.")))
            .Concat(selection.Cycles.Select(cycle => new RenderFinding(
                RenderFindingKind.PackageDependencyCycle,
                Join(cycle),
                $"Packages '{Join(cycle)}' depend on each other, so target '{target}' has no override priority order for them.")));
    }

    /// <summary>
    /// States what a target's shell selection could not settle.
    /// </summary>
    /// <param name="profile">The target, with its resolved package list.</param>
    /// <param name="application">The application being planned.</param>
    /// <param name="layoutName">The shell that was selected, or <see langword="null"/> when none was.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForLayout(
        SceneProfilesModel.UiProfile profile,
        SceneApplication application,
        string? layoutName,
        string target)
    {
        if (layoutName is null)
        {
            return profile.Layout is null
                ? [new RenderFinding(
                    RenderFindingKind.LayoutNotFound,
                    string.Empty,
                    $"The application declares no layout, so target '{target}' has no shell to render its screens inside.")]
                : [new RenderFinding(
                    RenderFindingKind.LayoutNotFound,
                    profile.Layout,
                    $"Target '{target}' selects layout '{profile.Layout}', which neither the application nor any package it activates declares.")];
        }

        // The translation binds every screen to the application's first shell, because a Scene screen holds a
        // single layout name. A target selecting a different one would render those screens inside a shell
        // they were never resolved against, so the mismatch is stated rather than rendered.
        return application.Screens
            .Where(screen => screen.Layout != layoutName)
            .Select(screen => new RenderFinding(
                RenderFindingKind.ScreenNotOnSelectedLayout,
                screen.Name,
                $"Screen '{screen.Name}' resolved against layout '{screen.Layout}', but target '{target}' renders inside '{layoutName}'."));
    }

    /// <summary>
    /// States what a target's theme selection could not settle.
    /// </summary>
    /// <param name="profile">The target, with its resolved package list.</param>
    /// <param name="theme">The theme that was found, or <see langword="null"/> when none was.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForTheme(SceneProfilesModel.UiProfile profile, SceneProfilesModel.Theme? theme, string target)
    {
        if (profile.Theme is null)
        {
            return [];
        }

        if (theme is null)
        {
            return [new RenderFinding(
                RenderFindingKind.ThemeNotFound,
                profile.Theme,
                $"Target '{target}' selects theme '{profile.Theme}', which the application does not declare.")];
        }

        return ThemeCompatibility.IncompatiblePackages(theme, profile)
            .Select(package => new RenderFinding(
                RenderFindingKind.ThemeIncompatible,
                package,
                $"Theme '{theme.Name}' is not declared compatible with package '{package}', which target '{target}' activates."));
    }

    /// <summary>
    /// States which component names bound to nothing.
    /// </summary>
    /// <param name="names">The component names that resolved against no active package.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForComponents(IEnumerable<string> names, string target) =>
        names.Select(name => new RenderFinding(
            RenderFindingKind.ComponentNotResolved,
            name,
            $"Component '{name}' resolves against none of the packages target '{target}' activates."));

    /// <summary>
    /// States what screen template placement could not settle.
    /// </summary>
    /// <param name="resolution">The outcome of nesting the application's templates inside the target's shell.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForScreenTemplates(ScreenTemplateResolution resolution, string target) =>
        resolution.Unplaced
            .Select(unplaced => new RenderFinding(
                RenderFindingKind.ScreenTemplateUnplaced,
                unplaced.Template,
                unplaced.Candidates.Count == 0
                    ? $"Screen template '{unplaced.Template}' fits slot '{unplaced.Slot}', which nothing target '{target}' renders declares."
                    : $"Screen template '{unplaced.Template}' fits slot '{unplaced.Slot}', which '{Join(unplaced.Candidates)}' all declare for target '{target}'."))
            .Concat(resolution.Cycles.Select(cycle => new RenderFinding(
                RenderFindingKind.ScreenTemplateCycle,
                Join(cycle),
                $"Screen templates '{Join(cycle)}' nest inside each other, so target '{target}' cannot build a tree from them.")));

    /// <summary>
    /// States which freeform arrangements had no variant for the size class a target renders at.
    /// </summary>
    /// <param name="structures">The structures, and slots within them, that selected no variant.</param>
    /// <param name="sizeClass">The size class the target renders at.</param>
    /// <param name="target">How the target is named in a message.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> ForSizeClass(IEnumerable<string> structures, SceneSizeClasses.SizeClass sizeClass, string target) =>
        structures.Select(structure => new RenderFinding(
            RenderFindingKind.SizeClassVariantMissing,
            structure,
            $"'{structure}' declares no freeform variant for the {sizeClass.Width}×{sizeClass.Height} size class target '{target}' renders at."));

    static string Join(IEnumerable<string> names) => string.Join(", ", names);
}
