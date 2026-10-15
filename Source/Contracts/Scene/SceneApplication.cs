// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using SceneExposure = Cratis.Scene.Model.Exposure;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneNavigation = Cratis.Scene.Model.Navigation;
using SceneProfiles = Cratis.Scene.Model.Profiles;
using SceneSampleData = Cratis.Scene.Model.SampleData;
using SceneScreens = Cratis.Scene.Model.Screens;
using SceneStructure = Cratis.Scene.Model.Structure;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The full result of translating a compiled Screenplay <c language="csharp">ApplicationSyntax</c> into Scene - part of
/// Cratis/Stage#37. A Stage-side bundle, not a Scene concept - <c language="csharp">Cratis.Scene.Model</c> has no "whole
/// translated app" type, and none should exist there.
/// </summary>
/// <param name="UiProfiles">Every <see cref="SceneProfiles.UiProfile"/> declared in the application.</param>
/// <param name="Themes">Every <see cref="SceneProfiles.Theme"/> declared in the application.</param>
/// <param name="Layouts">Every application <see cref="SceneLayouts.Layout"/> - the declared shells, or the single synthesized <see cref="DefaultLayout"/> when the document declares none.</param>
/// <param name="ScreenTemplates">Every <see cref="SceneScreens.ScreenTemplate"/> across every module.</param>
/// <param name="DialogTemplates">Every <see cref="SceneScreens.DialogTemplate"/> across every module.</param>
/// <param name="Screens">Every <see cref="SceneScreens.Screen"/> across every slice.</param>
public record SceneApplication(
    IReadOnlyList<SceneProfiles.UiProfile> UiProfiles,
    IReadOnlyList<SceneProfiles.Theme> Themes,
    IReadOnlyList<SceneLayouts.Layout> Layouts,
    IReadOnlyList<SceneScreens.ScreenTemplate> ScreenTemplates,
    IReadOnlyList<SceneScreens.DialogTemplate> DialogTemplates,
    IReadOnlyList<SceneScreens.Screen> Screens)
{
    /// <summary>
    /// Gets what each layout, screen template and dialog template lets an instance configure.
    /// </summary>
    public IReadOnlyList<SceneExposure.ExposureDeclaration> Exposures { get; init; } = [];

    /// <summary>
    /// Gets the values instances store for what was exposed, in declaration order.
    /// </summary>
    public IReadOnlyList<SceneExposure.InstanceContribution> InstanceContributions { get; init; } = [];

    /// <summary>
    /// Gets the application's topbar and sidebar and what each module inherits, extends or overrides. Empty when the document declares no region.
    /// </summary>
    public SceneNavigation.NavigationModel Navigation { get; init; } = new(new SceneNavigation.ApplicationNavigation([]), []);

    /// <summary>
    /// Gets the modules, features and slices (with each slice's kind) that navigation targets and toolbar buttons resolve against.
    /// A reference input to Scene's validation, never written into a scene document.
    /// </summary>
    [JsonIgnore]
    public SceneStructure.ApplicationStructure Structure { get; init; } = new([]);

    /// <summary>
    /// Gets the sample data packs the document declares - design and preview time rows, never part of a production build.
    /// </summary>
    public IReadOnlyList<SceneSampleData.SampleDataPack> SampleDataPacks { get; init; } = [];

    /// <summary>
    /// Gets the ids of the blueprint-shipped sample data packs a profile names - packs the build resolves against the packages the profile activates.
    /// </summary>
    public IReadOnlyList<string> BlueprintSampleDataPacks { get; init; } = [];

    /// <summary>
    /// Gets every template the document assigns or uses, as Scene template references, in declaration order.
    /// </summary>
    public IReadOnlyList<TemplateAssignment> TemplateReferences { get; init; } = [];

    /// <summary>
    /// Gets source constructs the Stage runtime cannot run, such as a guard it cannot evaluate. These are not Scene wire members.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<SceneRuntimeIssue> RuntimeIssues { get; init; } = [];
}
