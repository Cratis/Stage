// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiSyntaxAdmission;

public class when_auditing_the_ui_surface : Specification
{
    // Freeze the consumed owners as well as every new UI node. A dependency bump that adds a
    // member must make an explicit render/refuse decision rather than silently extending this list.
    // ReactionProductionMode and CommandReadMode are refused before legacy rendering/execution with STAGE-ESM-043 and STAGE-ESM-044.
    // StateMachines and Workflows (Screenplay 5.1.0) have no UI node: they reach Stage through the executable semantic model, where the runtime and the renderer decide.
    // Users (Screenplay 5.11.0, decision 0141) are development and test identities: the sandbox host reads them from the source to offer to Lens (SandboxUsers), and no rendered application carries them.
    // Agents (Screenplay 6.15.0) is the explicit agent operation allowlist. Screenplay binds a model that declares it as ESM v48 (v49 when it also uses caller identity), which CratisEsmSupport does not audit, so Stage refuses that model before rendering or running it and no rendered application exposes agent operations.
    // StickyNotes are Studio canvas notes: authoring metadata outside the executable model, so Stage neither renders nor refuses them.
    // TimeZone (Screenplay 4.247.0, decisions 0106/0107) never reaches Stage: the Screenplay binder refuses an application time zone (Cratis/Screenplay#824).
    // Context (Screenplay 5.3.1) is refused by the binder with PLAY0268 before Stage consumes it.
    // Graphs, GraphProjections and GraphQueries (Screenplay 5.7.0 and 5.8.0) render from the executable semantic model; the legacy syntax path refuses them with STAGE-ESM-067 (GraphSyntaxAdmission).
    // Nested sample-data rows (SampleDataFieldSyntax.Rows, SampleDataValueKind.Rows) are sample data for the Scene preview, which Stage does not render.
    // Screenplay 6: slots, outlets, fits and hooks are translated (SlotConverter, OutletConverter, OutletLevelConverter, ScreenConverter).
    // RetiredForms is source-span metadata of the Screenplay 5 forms the compiler refuses; it never reaches Stage.
    // Sinks (Screenplay 6.4.0, decision 0146) are realization metadata: Stage reads them from SemanticCompilation.Sinks, ReadModelSinks and GraphSinks, never from this syntax.
    // DesignSystems (Screenplay 6.4.0) are UI configuration the binder defers from the executable model (PLAY0269); Scene has no design system yet, so Stage neither renders nor refuses them.
    // Storylines (named journeys through the model), screen Visuals (sketches, mockup pages and images) and slice Placements (linked copies of elements declared elsewhere, `shows event <Name>`) are authoring metadata for the canvas: Stage neither renders nor refuses them, and none of them changes a command, query or event.
    // ScreenShowSyntax is the slot it stands in
    // (TemplateSlotPlacement); FormFieldSyntax.Choices is a plain input with a STAGE-SCENE-FORM-004 warning from UiSyntaxAdmission until Scene's form field can carry it.
    [Theory]
    [InlineData(typeof(ApplicationSyntax), "Agents Authentication Behaviors CommandReadMode Concepts Context DesignSystems Domain EventSources Examples Exposures FileImports Identity Imports InstanceContributions Layouts Modules Onboarding Personas Policies Purposes ReactionProductionMode RetiredForms SampleDataPacks Seeds Sinks SourceOptions StateMachines StickyNotes Storylines Systems Templates Themes TimeZone Triggers Types UiProfiles Users Workflows")]
    [InlineData(typeof(ModuleSyntax), "Authorize Behaviors Contributions DependsOn Description DialogTemplates Documentation Examples Features FileImports Forms IsPlacement Name Outlets Purposes RetiredForms ScreenTemplates Templates UsedBehaviors")]
    [InlineData(typeof(FeatureSyntax), "Authorize Behaviors Contributions DependsOn Description Documentation Examples Features FileImports IsPlacement Name Outlets Purposes RetiredForms Slices Templates UsedBehaviors")]
    [InlineData(typeof(SliceSyntax), "Captures Commands Constraints Description DescriptionLocation DescriptionRawLength Direction Documentation EffectiveDirection Events Examples File GraphProjections GraphQueries Graphs Name Operations Placements Projections Purposes Queries Reactions ReadModels Reducers Screens Specifications Templates Type")]
    [InlineData(typeof(InteractionBindingSyntax), "Actions Alternatives Condition Otherwise Trigger")]
    [InlineData(typeof(InteractionAlternativeSyntax), "Actions Condition")]
    [InlineData(typeof(InteractionOtherwiseSyntax), "Actions")]
    [InlineData(typeof(FormSyntax), "Behaviors ColumnMode Columns Description Fields For GenerationMode Layout Name OnSubmit Populate UsedBehaviors")]
    [InlineData(typeof(FormColumnSyntax), "Label Property")]
    [InlineData(typeof(LayoutSyntax), "Arrangement Behaviors Category Exposes Name Outlets RecursiveSlots RestrictsScopes RetiredForms Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(ScreenTemplateSyntax), "Arrangement Behaviors Category Content Description DisplayName Exposes FitsSlot FitsSlotLocation Name Outlets RecursiveSlots RestrictsScopes RetiredForms Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(DialogTemplateSyntax), "Arrangement Behaviors Category Content Description DisplayName Exposes Name Outlets RecursiveSlots RestrictsScopes RetiredForms Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(TemplateExposedValueSyntax), "Name Type")]
    [InlineData(typeof(TemplateSlotSyntax), "Name")]
    [InlineData(typeof(ScreenSyntax), "Contributions Description Directives File Fits Name Outlets Visuals")]
    [InlineData(typeof(ScreenNavigateSyntax), "By Parameters Route Screen Slot")]
    [InlineData(typeof(ExposureSyntax), "Owner Properties")]
    [InlineData(typeof(ExposedPropertySyntax), "Component EditableFields IsCollection Label Operations Path ReExposes RestrictsFields")]
    [InlineData(typeof(InstanceContributionsSyntax), "Contributions Instance")]
    [InlineData(typeof(InstanceContributionSyntax), "Component Items Path Value")]
    [InlineData(typeof(ContributedItemSyntax), "Id Values")]
    [InlineData(typeof(ContributedItemValueSyntax), "Field Value")]
    [InlineData(typeof(ScreenContributionSyntax), "ContributionPoint Directives Order")]
    [InlineData(typeof(ContributionSyntax), "ContributionPoint Destination Group Icon Id Items Label Navigate Order Presentation")]
    [InlineData(typeof(ContributionDestinationSyntax), "Kind Target")]
    [InlineData(typeof(TemplateSlotContentSyntax), "Directives Slot")]
    [InlineData(typeof(ArrangementContainerSyntax), "Children Columns Gap Grow Kind Rows Span")]
    [InlineData(typeof(ArrangementSlotSyntax), "Grow GrowFactor Height Name Span Width")]
    [InlineData(typeof(ScreenComponentSyntax), "Behaviors Component Context Exposes Icon Name Presentation Properties Slots StableId UsedBehaviors")]
    [InlineData(typeof(ComponentPropertySyntax), "Binding Property Value")]
    [InlineData(typeof(ComponentExposedValueSyntax), "Binding Name")]
    [InlineData(typeof(PresentationValueSyntax), "Name Value")]
    [InlineData(typeof(ComponentSlotSyntax), "Directives Name")]
    [InlineData(typeof(ScreenToolbarSyntax), "Buttons Items Name")]
    [InlineData(typeof(ToolbarButtonSyntax), "Authorize Confirm Emphasis EnabledWhen Icon Opens OpensSlot Runs Slice Subject")]
    [InlineData(typeof(OutletSyntax), "Hides Items Kind Logo Mode Name Slot Subject")]
    [InlineData(typeof(OutletHideSyntax), "Id")]
    [InlineData(typeof(OutletPositionSyntax), "Id Placement")]
    [InlineData(typeof(OutletItemSyntax), "Id IsDefault Position Title")]
    [InlineData(typeof(OutletGroupSyntax), "Buttons Icon")]
    [InlineData(typeof(OutletFilterSyntax), "Parameter Query")]
    [InlineData(typeof(OutletTabSyntax), "Shows")]
    [InlineData(typeof(OutletStatSyntax), "Field Query")]
    [InlineData(typeof(SlotSyntax), "Name Optional Primary Role Routed RoutedBy")]
    [InlineData(typeof(NavigationItemSyntax), "Children Icon Slot Target")]
    [InlineData(typeof(NavigationTargetSyntax), "Feature Module")]
    [InlineData(typeof(NavigationLogoSyntax), "Image Title")]

    // Screenplay decision 0110 / Cratis/Stage#855: explicit subjects render; unrendered inputs refuse with STAGE-ESM-059.
    [InlineData(typeof(ToolbarItemSyntax), "Confirm EnabledWhen Icon Kind Label Name Parameters Presentation Subject Target")]
    [InlineData(typeof(ScreenActionSyntax), "Command Inputs Label Navigate Subject")]
    [InlineData(typeof(ScreenNavigationParameterSyntax), "Binding Name")]
    [InlineData(typeof(TemplateAssignmentSyntax), "Blueprint Name Version")]
    [InlineData(typeof(RootTemplateSyntax), "Blueprint Name Options Version")]
    [InlineData(typeof(ShellOptionSyntax), "Kind Name Value")]
    [InlineData(typeof(ProfileAppearanceSyntax), "Accent AccentLocation ColorScheme ColorSchemeLocation Corners CornersLocation Font FontLocation")]
    [InlineData(typeof(ProfileIdentitySyntax), "LogoKind LogoLocation LogoValue Name NameLocation ShowName ShowNameLocation")]
    [InlineData(typeof(NewScreenTemplateSyntax), "Blueprint Name Version")]
    [InlineData(typeof(UiBindingSyntax), "BindingKind ComponentId ComponentPropertyPath ExpectedValueType Literal Mode NullBehavior Path Query RawText")]
    [InlineData(typeof(UiProfileSyntax), "Appearance DefaultSizeClass Icons Identity Layout Name NewScreenTemplate Packages Platforms Root SampleData Theme")]
    [InlineData(typeof(SampleDataPackSyntax), "Locale Name Sets Title")]
    [InlineData(typeof(SampleDataSetSyntax), "Rows Shape")]
    [InlineData(typeof(SampleDataRowSyntax), "Fields")]
    [InlineData(typeof(SampleDataFieldSyntax), "Kind Name Rows Value")]
    [InlineData(typeof(SampleDataSelectionSyntax), "Active Bindings Mappings")]
    [InlineData(typeof(SampleDataPackReferenceSyntax), "IsBlueprint Name")]
    [InlineData(typeof(SampleDataMappingSyntax), "ReadModel Shape")]
    [InlineData(typeof(SampleDataBindingSyntax), "Pack ReadModel")]
    public void should_require_a_decision_for_every_added_member(Type type, string audited)
    {
        var actual = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name).Where(name => name != nameof(SyntaxNode.Location)).Order(StringComparer.Ordinal);
        string.Join(' ', actual).ShouldEqual(audited);
    }

    [Theory]
    [InlineData(typeof(FormColumnMode), "Unspecified Auto Manual")]
    [InlineData(typeof(ToolbarItemKind), "Unknown Action Navigate Dialog")]
    [InlineData(typeof(SampleDataValueKind), "Text Number Flag Null Rows")]
    [InlineData(typeof(ShellOptionValueKind), "Word Text Number Flag")]
    [InlineData(typeof(ToolbarEmphasis), "Primary Secondary Quiet")]
    [InlineData(typeof(OutletKind), "Actions Filters Tabs Stats Navigation")]
    [InlineData(typeof(OutletMode), "Unspecified Inherit Extend Override")]
    [InlineData(typeof(OutletPlacement), "Before After")]
    [InlineData(typeof(SlotRole), "Page Pane Panel")]
    [InlineData(typeof(HookOpening), "Runs Dialog Drawer Slot")]
    [InlineData(typeof(ScreenFit), "Dialog Drawer Pane Panel Page")]
    [InlineData(typeof(UiBindingKind), "Invalid DataContext QueryResult ComponentProperty Literal")]
    [InlineData(typeof(ArrangementContainerKind), "Flat Row Column Grid")]
    [InlineData(typeof(ContributionDestinationKind), "Slot Dialog External")]
    [InlineData(typeof(UiBindingMode), "OneWay TwoWay")]
    [InlineData(typeof(UiBindingNullBehavior), "Propagate Clear Preserve")]
    public void should_require_a_decision_for_every_added_variant(Type type, string audited) => string.Join(' ', Enum.GetNames(type)).ShouldEqual(audited);
}
