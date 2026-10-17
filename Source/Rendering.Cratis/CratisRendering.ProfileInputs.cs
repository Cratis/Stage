// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// The diagnostic code for a ui profile that activates a package the target does not render.
    /// </summary>
    public const string UnknownPackageCode = "STAGE-PROFILE-001";

    /// <summary>
    /// The diagnostic code for a ui profile that selects a layout neither the application nor an active package declares.
    /// </summary>
    public const string MissingLayoutCode = "STAGE-PROFILE-002";

    /// <summary>
    /// The diagnostic code for a ui profile whose packages cannot be used together: a theme not compatible with an active
    /// package, or a package dependency the catalog cannot satisfy.
    /// </summary>
    public const string IncompatiblePackageCode = "STAGE-PROFILE-003";

    /// <summary>
    /// The diagnostic code for a ui profile that selects a theme the application does not declare.
    /// </summary>
    public const string MissingThemeCode = "STAGE-PROFILE-004";

    /// <summary>
    /// The diagnostic code for a ui profile whose root template cannot be the application's shell: it does not exist, its
    /// blueprint is not installed, its version pin is not satisfied by the installed blueprint or it has no content outlet.
    /// </summary>
    public const string RootTemplateCode = "STAGE-PROFILE-005";

    /// <summary>
    /// The diagnostic code for a template blueprint reference to a blueprint, a version of it or a template that the
    /// approved blueprint does not provide, which would otherwise render a screen with no template.
    /// </summary>
    public const string UnresolvedTemplateCode = "STAGE-PROFILE-006";

    /// <summary>
    /// Resolves every ui profile of an authored Scene against the packages this target renders and reports the profile inputs it
    /// cannot honor.
    /// </summary>
    /// <param name="scene">The authored Scene.</param>
    /// <param name="approvedPackages">The custom runtime packages the application approved, or null for none.</param>
    /// <returns>One finding for each distinct profile input the target cannot honor; empty when every profile resolves.</returns>
    /// <remarks>
    /// <para>
    /// The resolution rules are the Scene engine's <see cref="RenderPlanner"/>, used unmodified, so no consumer holds a second
    /// answer to which package satisfies a profile or which layout a profile renders inside. Screenplay reports a missing layout
    /// or an incompatible theme as a warning about source text; for a render they mean the published application would silently
    /// use a different shell or unstyled components, so every consumer that renders - the CLI and Studio alike - refuses before
    /// it writes or generates anything.
    /// </para>
    /// <para>
    /// Only findings about the profile's own inputs refuse. Component, screen-template and size-class findings describe how a
    /// valid profile resolves, and the renderer reports or tolerates those on its own terms.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<CratisProfileInputFinding> ResolveProfileInputs(SceneApplication scene, ApprovedPackageSet? approvedPackages = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var plan = RenderPlanner.Plan(scene, CatalogWith(approvedPackages));
        return
        [
            .. plan.Targets
                .SelectMany(target => target.Findings)
                .Concat(plan.Findings)
                .Select(finding => (Finding: finding, Code: ProfileInputCodeFor(finding.Kind)))
                .Where(_ => _.Code is not null)
                .DistinctBy(_ => (_.Finding.Kind, _.Finding.Subject, _.Finding.Message))
                .Select(_ => new CratisProfileInputFinding(_.Finding.Kind, _.Code!, _.Finding.Subject, _.Finding.Message))
        ];
    }

    static string? ProfileInputCodeFor(RenderFindingKind kind) => kind switch
    {
        RenderFindingKind.PackageNotInCatalog => UnknownPackageCode,
        RenderFindingKind.LayoutNotFound => MissingLayoutCode,
        RenderFindingKind.ThemeIncompatible or
            RenderFindingKind.PackageDependencyMissing or
            RenderFindingKind.PackageVersionConflict or
            RenderFindingKind.PackageDependencyCycle => IncompatiblePackageCode,
        RenderFindingKind.ThemeNotFound => MissingThemeCode,
        RenderFindingKind.RootTemplateInvalid => RootTemplateCode,
        RenderFindingKind.TemplateReferenceUnresolved => UnresolvedTemplateCode,
        _ => null
    };
}
