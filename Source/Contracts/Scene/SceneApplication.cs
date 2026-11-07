// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneExposure = Cratis.Scene.Model.Exposure;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneNavigation = Cratis.Scene.Model.Navigation;
using SceneOutlets = Cratis.Scene.Model.Outlets;
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
/// <param name="Layouts">Every declared application <see cref="SceneLayouts.Layout"/>. Empty when the blueprint supplies the shell.</param>
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
    /// Gets the typed command-response and destination metadata consumed by Scene's form validator.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public System.Text.Json.Nodes.JsonObject? ResponseContracts { get; init; }

    /// <summary>
    /// Gets the values instances store for what was exposed, in declaration order.
    /// </summary>
    public IReadOnlyList<SceneExposure.InstanceContribution> InstanceContributions { get; init; } = [];

    /// <summary>
    /// Gets the application's navigation outlets and what each module refines or contributes to them. Empty when the document declares no
    /// navigation outlet.
    /// </summary>
    public SceneNavigation.NavigationModel Navigation { get; init; } = new(new SceneNavigation.ApplicationNavigation([]), []);

    /// <summary>
    /// Gets the outlet levels of the modules, features and slice screens that refine or contribute to an outlet - the host's levels Scene's
    /// engine resolves an outlet through (see <see cref="StageOutletChains"/>). Null when no level changes an outlet.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<Cratis.Scene.Engine.Outlets.OutletLevel>? OutletLevels { get; init; }

    /// <summary>
    /// Gets the modules, features and slices (with each slice's kind) that navigation targets and toolbar buttons resolve against.
    /// A reference input to Scene's validation. It is written to a scene document only where a runtime needs it: the Stage's runtime
    /// document and a live Stage's scene, so the shell's navigation regions and a toolbar's slice dialogs resolve against it.
    /// </summary>
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

    /// <summary>
    /// Gets source constructs omitted during conversion. Errors prevent rendering, but importers may retain the partial contract.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<ConversionDiagnostic> ConversionDiagnostics { get; init; } = [];

    /// <summary>
    /// Gets the JSON schema of every modeled command, by command name, which a toolbar slice's task dialog reads its fields from. Set where the
    /// semantic model is known - a live Stage and the generated Stage runtime document - and absent from the canonical document.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CommandSchemas { get; init; }

    /// <summary>
    /// Gets whether a toolbar button of this application names a slice, which is when its runtime needs the structure and the command
    /// schemas to open the slice as a task dialog.
    /// </summary>
    [JsonIgnore]
    public bool ReferencesSlices =>
        Screens.Any(screen => screen.SlotContent.Values.Any(HasSliceButton) || screen.Contributions.Any(contribution => HasSliceButton([contribution.Content]))) ||
        ScreenTemplates.Any(template => template.Content is { } content && content.Values.Any(HasSliceButton)) ||
        DialogTemplates.Any(template => template.Content is { } content && content.Values.Any(HasSliceButton)) ||
        SliceButtons().Any();

    /// <summary>
    /// Gets every outlet button that names a slice (see <see cref="OutletButtons"/>).
    /// </summary>
    /// <returns>The <see cref="SceneOutlets.OutletButton"/>s that name a slice, including those in groups.</returns>
    public IEnumerable<SceneOutlets.OutletButton> SliceButtons() => OutletButtons().Where(button => button.Slice is { Length: > 0 });

    /// <summary>
    /// Gets every outlet button: the defaults of the outlets the layouts, templates and navigation declare, what module, feature and slice
    /// levels add to them, and the items of the outlets screens place.
    /// </summary>
    /// <returns>The <see cref="SceneOutlets.OutletButton"/>s, including those in groups.</returns>
    public IEnumerable<SceneOutlets.OutletButton> OutletButtons()
    {
        var declared = Layouts.SelectMany(layout => layout.Outlets ?? [])
            .Concat(ScreenTemplates.SelectMany(template => template.Outlets ?? []))
            .Concat(DialogTemplates.SelectMany(template => template.Outlets ?? []))
            .Concat(Navigation.Application.Outlets)
            .SelectMany(outlet => outlet.Defaults);
        var refined = (OutletLevels ?? [])
            .SelectMany(level => ItemsOf(level.Refinements, level.Contributions))
            .Concat(Navigation.Modules.SelectMany(module => ItemsOf(module.Refinements, module.Contributions)));
        var placed = Screens.SelectMany(screen => screen.SlotContent.Values.SelectMany(PlacedItems))
            .Concat(ScreenTemplates.SelectMany(template => (template.Content?.Values ?? []).SelectMany(PlacedItems)))
            .Concat(DialogTemplates.SelectMany(template => (template.Content?.Values ?? []).SelectMany(PlacedItems)));
        return declared.Concat(refined).Concat(placed)
            .SelectMany(item => item is SceneOutlets.OutletGroup group ? group.Items : [item])
            .OfType<SceneOutlets.OutletButton>();
    }

    /// <summary>
    /// Gets this application as a running Stage serves it: without sample data (spec SD6). Packs, the blueprint packs it names and the profile's
    /// selection exist to preview with at design time; a production render never carries them.
    /// </summary>
    /// <returns>The application with no sample data.</returns>
    public SceneApplication ForRuntime() => this with
    {
        SampleDataPacks = [],
        BlueprintSampleDataPacks = [],
        UiProfiles = [.. UiProfiles.Select(profile => profile with { SampleData = null })]
    };

    static IEnumerable<SceneOutlets.OutletItem> ItemsOf(IReadOnlyList<SceneOutlets.OutletRefinement>? refinements, IReadOnlyList<SceneOutlets.OutletContribution>? contributions) =>
        (refinements ?? []).SelectMany(refinement => refinement.Items ?? []).Concat((contributions ?? []).SelectMany(contribution => contribution.Items));

    // The items of the outlets a screen or template places with core:outletItems, wherever they sit.
    static IEnumerable<SceneOutlets.OutletItem> PlacedItems(IReadOnlyList<SceneElements.SceneElement> elements) =>
        elements.OfType<SceneElements.ExternalComponent>().SelectMany(component =>
            (component.ComponentName == ScreenDirectiveConverter.OutletItemsComponent && component.Properties.TryGetValue("items", out var items) && items is IEnumerable<SceneOutlets.OutletItem> outletItems ? outletItems : [])
                .Concat(component.Slots.Values.SelectMany(PlacedItems)));

    static bool HasSliceButton(IReadOnlyList<SceneElements.SceneElement> elements) =>
        elements.Any(element => element is SceneElements.ExternalComponent component &&
            ((component.Properties.TryGetValue("slice", out var slice) && slice is string { Length: > 0 }) || component.Slots.Values.Any(HasSliceButton)));
}
