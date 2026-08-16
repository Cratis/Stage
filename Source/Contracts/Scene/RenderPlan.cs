// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneModelProfiles = Cratis.Scene.Model.Profiles;
using ScenePackages = Cratis.Scene.Engine.Packages;
using SceneProfiles = Cratis.Scene.Engine.Profiles;
using SceneScreensEngine = Cratis.Scene.Engine.Screens;
using SceneSizeClasses = Cratis.Scene.Model.SizeClasses;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Everything one deployment target resolves to - a <c>ui profile</c> on one platform, with its packages
/// expanded, its components bound, its shell and theme picked and its arrangements evaluated. Cratis/Stage#39.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SceneApplication"/> is deliberately target-agnostic: a screen names components and slots, never
/// a package, a theme or a shell. A render plan is that same application made concrete for exactly one target,
/// and it is where a build finds out whether the target can actually be rendered. Every resolution decision in
/// it was made by <c>Cratis.Scene.Engine</c>, so a plan and a running renderer resolve identically.
/// </para>
/// <para>
/// Nothing unresolved is dropped: whatever did not resolve appears in <see cref="Findings"/>, so a plan is
/// never quietly smaller than the application it came from.
/// </para>
/// </remarks>
/// <param name="Profile">
/// The target, with <see cref="SceneModelProfiles.UiProfile.Packages"/> replaced by the resolved transitive
/// closure in ascending override-priority order - the list every other resolution in this plan ran against.
/// The originally declared subset is <see cref="Packages"/> minus its <see cref="ScenePackages.PackageSelection.Added"/>.
/// </param>
/// <param name="Packages">The outcome of expanding the target's declared packages against the catalog.</param>
/// <param name="LayoutName">The name of the shell this target renders inside, or <see langword="null"/> when nothing in scope declares the one it selects.</param>
/// <param name="Layout">
/// The shell's structure, when the application declares it. <see langword="null"/> while
/// <paramref name="LayoutName"/> is set means an active package provides the shell: a catalog carries package
/// names and the names of what they contribute, never structures, so this plan places no screen templates and
/// evaluates no arrangements for it.
/// </param>
/// <param name="Theme">The theme this target applies, or <see langword="null"/> when it selects none or names one the application does not declare.</param>
/// <param name="ThemePackages">The active packages the theme's tokens actually apply to - what a renderer scopes token application to, rather than applying them globally.</param>
/// <param name="SizeClass">The size class this target's arrangements were evaluated at.</param>
/// <param name="Components">Every component name the application references that resolved, with the packages each one shadowed.</param>
/// <param name="ScreenTemplates">How the application's screen templates nest inside <paramref name="Layout"/>.</param>
/// <param name="Arrangements">What each slot-bearing structure's arrangement resolves to at <paramref name="SizeClass"/>.</param>
/// <param name="Findings">Everything this target could not fully resolve.</param>
public record RenderPlan(
    SceneModelProfiles.UiProfile Profile,
    ScenePackages.PackageSelection Packages,
    string? LayoutName,
    SceneLayouts.Layout? Layout,
    SceneModelProfiles.Theme? Theme,
    IReadOnlyList<string> ThemePackages,
    SceneSizeClasses.SizeClass SizeClass,
    IReadOnlyList<SceneProfiles.ComponentResolution> Components,
    SceneScreensEngine.ScreenTemplateResolution ScreenTemplates,
    IReadOnlyList<ArrangementSelection> Arrangements,
    IReadOnlyList<RenderFinding> Findings)
{
    /// <summary>
    /// Gets a value indicating whether everything this target needs resolved, resolved.
    /// </summary>
    public bool IsComplete => Findings.Count == 0;
}
