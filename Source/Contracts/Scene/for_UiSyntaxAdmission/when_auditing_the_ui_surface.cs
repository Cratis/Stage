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
    [Theory]
    [InlineData(typeof(ApplicationSyntax), "Authentication Behaviors Concepts Domain EventSources Examples FileImports Imports Layouts Modules Personas Policies Purposes Seeds SourceOptions Systems Templates Themes Triggers Types UiProfiles")]
    [InlineData(typeof(ModuleSyntax), "Authorize Behaviors Contributions DependsOn Description DialogTemplates Documentation Examples Features FileImports Forms IsPlacement Name Purposes ScreenTemplates Templates UsedBehaviors")]
    [InlineData(typeof(FeatureSyntax), "Authorize Behaviors Contributions DependsOn Description Documentation Examples Features FileImports IsPlacement Name Purposes Slices Templates UsedBehaviors")]
    [InlineData(typeof(SliceSyntax), "Captures Commands Constraints Description DescriptionLocation DescriptionRawLength Direction Documentation EffectiveDirection Events Examples File Name Operations Projections Purposes Queries Reactions ReadModels Reducers Screens Specifications Templates Type")]
    [InlineData(typeof(InteractionBindingSyntax), "Actions Alternatives Condition Otherwise Trigger")]
    [InlineData(typeof(InteractionAlternativeSyntax), "Actions Condition")]
    [InlineData(typeof(InteractionOtherwiseSyntax), "Actions")]
    [InlineData(typeof(FormSyntax), "Behaviors ColumnMode Columns Description Fields For GenerationMode Layout Name OnSubmit Populate UsedBehaviors")]
    [InlineData(typeof(FormColumnSyntax), "Label Property")]
    [InlineData(typeof(LayoutSyntax), "Arrangement Behaviors Category Exposes Name Outlets Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(ScreenTemplateSyntax), "Arrangement Behaviors Category Exposes FitsSlot FitsSlotLocation Name Outlets Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(DialogTemplateSyntax), "Arrangement Behaviors Category Exposes Name Outlets Slots TemplateType UsedBehaviors")]
    [InlineData(typeof(TemplateExposedValueSyntax), "Name Type")]
    [InlineData(typeof(TemplateOutletSyntax), "Name")]
    [InlineData(typeof(ScreenSyntax), "Description Directives File Name")]
    [InlineData(typeof(ScreenNavigateSyntax), "By Parameters Route Screen")]
    [InlineData(typeof(ScreenComponentSyntax), "Behaviors Component Context Exposes Icon Name Outlets Presentation Properties StableId UsedBehaviors")]
    [InlineData(typeof(ComponentPropertySyntax), "Binding Property Value")]
    [InlineData(typeof(ComponentExposedValueSyntax), "Binding Name")]
    [InlineData(typeof(PresentationValueSyntax), "Name Value")]
    [InlineData(typeof(ComponentOutletSyntax), "Directives Name")]
    [InlineData(typeof(ScreenToolbarSyntax), "Items Name")]
    [InlineData(typeof(ToolbarItemSyntax), "Icon Kind Label Name Parameters Presentation Target")]
    [InlineData(typeof(ScreenNavigationParameterSyntax), "Binding Name")]
    [InlineData(typeof(TemplateAssignmentSyntax), "Name")]
    [InlineData(typeof(UiBindingSyntax), "BindingKind ComponentId ComponentPropertyPath ExpectedValueType Literal Mode NullBehavior Path Query RawText")]
    [InlineData(typeof(UiProfileSyntax), "DefaultSizeClass Icons Layout Name Packages Platforms Theme")]
    public void should_require_a_decision_for_every_added_member(Type type, string audited)
    {
        var actual = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name).Where(name => name != nameof(SyntaxNode.Location)).Order(StringComparer.Ordinal);
        string.Join(' ', actual).ShouldEqual(audited);
    }

    [Theory]
    [InlineData(typeof(FormColumnMode), "Unspecified Auto Manual")]
    [InlineData(typeof(ToolbarItemKind), "Unknown Action Navigate Dialog")]
    [InlineData(typeof(UiBindingKind), "Invalid DataContext QueryResult ComponentProperty Literal")]
    [InlineData(typeof(UiBindingMode), "OneWay TwoWay")]
    [InlineData(typeof(UiBindingNullBehavior), "Propagate Clear Preserve")]
    public void should_require_a_decision_for_every_added_variant(Type type, string audited) => string.Join(' ', Enum.GetNames(type)).ShouldEqual(audited);
}
