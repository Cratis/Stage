// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_the_application_declares_a_layout_and_templates : Specification
{
    ApplicationSyntax _syntax = null!;
    SceneApplication _result = null!;

    void Establish()
    {
        var layout = new LayoutSyntax(
            "AppShell",
            [new SlotSyntax("navigation", "Navigation", SourceLocation.Start), new SlotSyntax("content", null, SourceLocation.Start)],
            SourceLocation.Start);

        var screenTemplate = new ScreenTemplateSyntax(
            "MasterDetail",
            [new SlotSyntax("sidebar", null, SourceLocation.Start), new SlotSyntax("main", null, SourceLocation.Start)],
            SourceLocation.Start,
            FitsSlot: "content");

        var dialogTemplate = new DialogTemplateSyntax(
            "RegisterInvoiceDialog",
            [new SlotSyntax("body", null, SourceLocation.Start)],
            SourceLocation.Start);

        var screen = new ScreenSyntax(
            "InvoiceDetails",
            null,
            [new ScreenTemplateReferenceSyntax("MasterDetail", [new ScreenSlotSyntax("main", [new ScreenTitleSyntax("Details", SourceLocation.Start)], SourceLocation.Start)], SourceLocation.Start)],
            SourceLocation.Start);

        var slice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "InvoiceDetails", [], [], [], [], [], [], [screen], [], [], SourceLocation.Start);
        var feature = new FeatureSyntax("InvoiceManagement", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Invoicing", [screenTemplate], [feature], SourceLocation.Start, DialogTemplates: [dialogTemplate]);

        _syntax = new([], [], [], [module], SourceLocation.Start, Layouts: [layout]);
    }

    void Because() => _result = new ScreenplaySceneVisitor().Visit(_syntax);

    [Fact] void should_convert_the_application_layout() => _result.Layouts.Single().Name.ShouldEqual("AppShell");
    [Fact] void should_convert_the_modules_screen_template() => _result.ScreenTemplates.Single().Name.ShouldEqual("MasterDetail");
    [Fact] void should_carry_the_screen_templates_fits_slot() => _result.ScreenTemplates.Single().FitsSlot.ShouldEqual("content");
    [Fact] void should_convert_the_modules_dialog_template() => _result.DialogTemplates.Single().Name.ShouldEqual("RegisterInvoiceDialog");
    [Fact] void should_convert_the_slices_screen() => _result.Screens.Single().Name.ShouldEqual("InvoiceDetails");
    [Fact] void should_resolve_the_screen_against_the_declared_layout() => _result.Screens.Single().Layout.ShouldEqual("AppShell");
    [Fact] void should_name_the_screen_template_the_screen_fills() => _result.Screens.Single().ScreenTemplate.ShouldEqual("MasterDetail");
}
