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

    [Fact] void should_not_invent_an_application_layout() => _result.Layouts.ShouldBeEmpty();
    [Fact] void should_leave_every_screens_shell_for_the_blueprint_to_resolve() => _result.Screens.All(screen => string.IsNullOrEmpty(screen.Layout)).ShouldBeTrue();
    [Fact] void should_leave_every_screens_template_unset() => _result.Screens.All(screen => screen.ScreenTemplate is null).ShouldBeTrue();
    [Fact] void should_convert_both_screens() => _result.Screens.Count.ShouldEqual(2);
}
