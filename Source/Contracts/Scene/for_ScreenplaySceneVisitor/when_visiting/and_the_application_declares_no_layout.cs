// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_the_application_declares_no_layout : Specification
{
    ApplicationSyntax _syntax = null!;
    SceneApplication _result = null!;

    void Establish()
    {
        var first = new ScreenSyntax("InvoiceList", null, [new ScreenTitleSyntax("Invoices", SourceLocation.Start)], SourceLocation.Start);
        var second = new ScreenSyntax("CustomerList", null, [new ScreenTitleSyntax("Customers", SourceLocation.Start)], SourceLocation.Start);

        var slice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [], [], [], [first, second], [], [], SourceLocation.Start);
        var feature = new FeatureSyntax("Listing", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Invoicing", [], [feature], SourceLocation.Start);

        _syntax = new([], [], [], [module], SourceLocation.Start);
    }

    void Because() => _result = new ScreenplaySceneVisitor().Visit(_syntax);

    [Fact] void should_synthesize_a_single_shell_for_the_whole_application() => _result.Layouts.Count.ShouldEqual(1);
    [Fact] void should_name_the_synthesized_shell_after_the_application() => _result.Layouts[0].Name.ShouldEqual(DefaultLayout.Name);
    [Fact] void should_give_the_synthesized_shell_a_single_content_slot() => _result.Layouts[0].Slots.Single().Name.ShouldEqual(DefaultLayout.ContentSlotName);
    [Fact] void should_resolve_every_screen_against_the_same_synthesized_shell() => _result.Screens.All(screen => screen.Layout == DefaultLayout.Name).ShouldBeTrue();
    [Fact] void should_leave_every_screens_template_unset() => _result.Screens.All(screen => screen.ScreenTemplate is null).ShouldBeTrue();
    [Fact] void should_convert_both_screens() => _result.Screens.Count.ShouldEqual(2);
}
