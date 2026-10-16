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
    [Theory]
    [InlineData(typeof(ApplicationSyntax), "Authentication Behaviors CommandReadMode Concepts Domain EventSources Examples Exposures FileImports Identity Imports InstanceContributions Layouts Modules Personas Policies Purposes ReactionProductionMode SampleDataPacks Seeds SourceOptions Systems Templates Themes Triggers Types UiProfiles")]
    [InlineData(typeof(ModuleSyntax), "Authorize Behaviors Contributions DependsOn Description DialogTemplates Documentation Examples Features FileImports Forms IsPlacement Name Navigation Purposes ScreenTemplates Templates UsedBehaviors")]
    [InlineData(typeof(FeatureSyntax), "Authorize Behaviors Contributions DependsOn Description Documentation Examples Features FileImports IsPlacement Name Purposes Slices Templates UsedBehaviors")]
    [InlineData(typeof(SliceSyntax), "Captures Commands Constraints Description DescriptionLocation DescriptionRawLength Direction Documentation EffectiveDirection Events Examples File Name Operations Projections Purposes Queries Reactions ReadModels Reducers Screens Specifications Templates Type")]
    [InlineData(typeof(InteractionBindingSyntax), "Actions Alternatives Condition Otherwise Trigger")]
    [InlineData(typeof(InteractionAlternativeSyntax), "Actions Condition")]
    [InlineData(typeof(InteractionOtherwiseSyntax), "Actions")]
    [InlineData(typeof(FormSyntax), "Behaviors ColumnMode Columns Description Fields For GenerationMode Layout Name OnSubmit Populate UsedBehaviors")]
    [InlineData(typeof(FormColumnSyntax), "Label Property")]
    [InlineData(typeof(LayoutSyntax), "Arrangement Behaviors Category Exposes Name Navigation Outlets RestrictsScopes Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(ScreenTemplateSyntax), "Arrangement Behaviors Category Content Description DisplayName Exposes FitsSlot FitsSlotLocation Name Outlets RestrictsScopes Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(DialogTemplateSyntax), "Arrangement Behaviors Category Content Description DisplayName Exposes Name Outlets RestrictsScopes Scopes SliceKinds Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(TemplateExposedValueSyntax), "Name Type")]
    [InlineData(typeof(TemplateOutletSyntax), "Name")]
    [InlineData(typeof(ScreenSyntax), "Contributions Description Directives File Name")]
    [InlineData(typeof(ScreenNavigateSyntax), "By Outlet Parameters Route Screen")]
    [InlineData(typeof(ExposureSyntax), "Owner Properties")]
    [InlineData(typeof(ExposedPropertySyntax), "Component EditableFields IsCollection Label Operations Path ReExposes RestrictsFields")]
    [InlineData(typeof(InstanceContributionsSyntax), "Contributions Instance")]
    [InlineData(typeof(InstanceContributionSyntax), "Component Items Path Value")]
    [InlineData(typeof(ContributedItemSyntax), "Id Values")]
    [InlineData(typeof(ContributedItemValueSyntax), "Field Value")]
    [InlineData(typeof(ScreenContributionSyntax), "ContributionPoint Directives Order")]
    [InlineData(typeof(ContributionSyntax), "ContributionPoint Destination Group Icon Id Label Navigate Order Presentation")]
    [InlineData(typeof(ContributionDestinationSyntax), "Kind Target")]
    [InlineData(typeof(TemplateSlotContentSyntax), "Directives Slot")]
    [InlineData(typeof(ArrangementContainerSyntax), "Children Columns Gap Grow Kind Rows Span")]
    [InlineData(typeof(ArrangementSlotSyntax), "Grow GrowFactor Height Name Span Width")]
    [InlineData(typeof(ScreenComponentSyntax), "Behaviors Component Context Exposes Icon Name Outlets Presentation Properties StableId UsedBehaviors")]
    [InlineData(typeof(ComponentPropertySyntax), "Binding Property Value")]
    [InlineData(typeof(ComponentExposedValueSyntax), "Binding Name")]
    [InlineData(typeof(PresentationValueSyntax), "Name Value")]
    [InlineData(typeof(ComponentOutletSyntax), "Directives Name")]
    [InlineData(typeof(ScreenToolbarSyntax), "Buttons Items Name")]
    [InlineData(typeof(ToolbarButtonSyntax), "Emphasis Icon Id Slice Title")]
    [InlineData(typeof(NavigationRegionSyntax), "Items Logo Mode Region")]
    [InlineData(typeof(NavigationItemSyntax), "Children Icon Id Slot Target Title")]
    [InlineData(typeof(NavigationTargetSyntax), "Feature Module")]
    [InlineData(typeof(NavigationLogoSyntax), "Image Title")]
    [InlineData(typeof(ToolbarItemSyntax), "Icon Kind Label Name Parameters Presentation Target")]
    [InlineData(typeof(ScreenNavigationParameterSyntax), "Binding Name")]
    [InlineData(typeof(TemplateAssignmentSyntax), "Blueprint Name Version")]
    [InlineData(typeof(UiBindingSyntax), "BindingKind ComponentId ComponentPropertyPath ExpectedValueType Literal Mode NullBehavior Path Query RawText")]
    [InlineData(typeof(UiProfileSyntax), "DefaultSizeClass Icons Layout Name Packages Platforms SampleData Theme")]
    [InlineData(typeof(SampleDataPackSyntax), "Locale Name Sets Title")]
    [InlineData(typeof(SampleDataSetSyntax), "Rows Shape")]
    [InlineData(typeof(SampleDataRowSyntax), "Fields")]
    [InlineData(typeof(SampleDataFieldSyntax), "Kind Name Value")]
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
    [InlineData(typeof(SampleDataValueKind), "Text Number Flag Null")]
    [InlineData(typeof(ToolbarEmphasis), "Primary Secondary Quiet")]
    [InlineData(typeof(NavigationRegionKind), "Topbar Sidebar")]
    [InlineData(typeof(NavigationRegionMode), "Unspecified Inherit Extend Override")]
    [InlineData(typeof(UiBindingKind), "Invalid DataContext QueryResult ComponentProperty Literal")]
    [InlineData(typeof(ArrangementContainerKind), "Flat Row Column Grid")]
    [InlineData(typeof(ContributionDestinationKind), "Outlet Dialog External")]
    [InlineData(typeof(UiBindingMode), "OneWay TwoWay")]
    [InlineData(typeof(UiBindingNullBehavior), "Propagate Clear Preserve")]
    public void should_require_a_decision_for_every_added_variant(Type type, string audited) => string.Join(' ', Enum.GetNames(type)).ShouldEqual(audited);
}
