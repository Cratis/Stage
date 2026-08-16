// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// What a <see cref="RenderFinding"/> is about - part of Cratis/Stage#39.
/// </summary>
/// <remarks>
/// Every member is a <em>resolution</em> outcome, discovered after a clean compile: the Screenplay source is
/// syntactically valid and semantically complete on its own, and only turns out to be under-specified once it
/// is resolved against a concrete target's packages, theme and layout. That is a different class of problem
/// from a compilation error - <see cref="EventModelLoader"/> throws <see cref="InvalidEventModel"/> for those
/// before any of this runs - which is why these are reported on the result rather than thrown or pushed back
/// into Screenplay's diagnostic channel.
/// </remarks>
public enum RenderFindingKind
{
    /// <summary>
    /// The application declares no <c>ui profile</c>, so there is no deployment target to render for.
    /// </summary>
    NoTargetDeclared = 0,

    /// <summary>
    /// A package the target activates has no declaration in the catalog, so nothing it contributes -
    /// components, layouts, templates, themes - can be resolved.
    /// </summary>
    PackageNotInCatalog = 1,

    /// <summary>
    /// A package the target activates depends on a package nothing in the catalog can satisfy, so the
    /// selection is incomplete rather than merely under-specified.
    /// </summary>
    PackageDependencyMissing = 2,

    /// <summary>
    /// A dependency resolved by name to a package the catalog contains, but at a version its declared range
    /// does not accept.
    /// </summary>
    PackageVersionConflict = 3,

    /// <summary>
    /// Packages depend on each other in a cycle, so no override priority order exists for them and which one
    /// wins a component name collision is undefined.
    /// </summary>
    PackageDependencyCycle = 4,

    /// <summary>
    /// The target selects a layout by name that neither the application nor any active package declares, so
    /// there is no shell to render its screens inside.
    /// </summary>
    LayoutNotFound = 5,

    /// <summary>
    /// A screen resolved against a different layout than the one this target selects. The translation binds
    /// every screen to the application's first declared shell; a target selecting another one would render
    /// that screen inside a shell it was never resolved against.
    /// </summary>
    ScreenNotOnSelectedLayout = 6,

    /// <summary>
    /// The target selects a theme by name that the application does not declare, so no tokens apply.
    /// </summary>
    ThemeNotFound = 7,

    /// <summary>
    /// The target's theme is not declared compatible with one of the packages the target activates, so that
    /// package's components render unthemed or wrongly themed.
    /// </summary>
    ThemeIncompatible = 8,

    /// <summary>
    /// A component name the application references resolves against nothing in the target's package list -
    /// the screen has a hole in it where that component should be.
    /// </summary>
    ComponentNotResolved = 9,

    /// <summary>
    /// A screen template's <c>fits slot</c> names no single container - either nothing in scope declares a
    /// slot of that name, or several do and the name is ambiguous.
    /// </summary>
    ScreenTemplateUnplaced = 10,

    /// <summary>
    /// Screen templates nest inside each other in a cycle, so no depth can be assigned and no tree built.
    /// </summary>
    ScreenTemplateCycle = 11,

    /// <summary>
    /// A freeform arrangement declares no placement variant for the size class this target renders at.
    /// Freeform deliberately has no fallback variant, so the affected slots have nowhere to go.
    /// </summary>
    SizeClassVariantMissing = 12
}
