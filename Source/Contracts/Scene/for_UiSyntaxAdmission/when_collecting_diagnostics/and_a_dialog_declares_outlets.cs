// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiSyntaxAdmission.when_collecting_diagnostics;

public class and_a_dialog_declares_outlets : Specification
{
    [Fact]
    public void should_report_the_member_and_keep_the_remaining_scene()
    {
        var location = new SourceLocation(4, 3, "dialogs.play");
        var dialog = new DialogTemplateSyntax("Help", [], location) { Outlets = [new TemplateOutletSyntax("detail", location)] };
        var module = new ModuleSyntax("Projects", [], [], location) { DialogTemplates = [dialog, new DialogTemplateSyntax("Good", [], location)] };
        var syntax = new ApplicationSyntax([], [], [], [module], location);
        var result = new ScreenplaySceneVisitor().Convert(syntax);

        result.DialogTemplates.Count.ShouldEqual(2);
        result.ConversionDiagnostics.Single().Code.ShouldEqual(UnsupportedUiSyntax.DiagnosticCode);
        result.ConversionDiagnostics.Single().Construct.ShouldEqual("DialogTemplateSyntax.Outlets");
        result.ConversionDiagnostics.Single().Path.ShouldEqual("Help");
        result.ConversionDiagnostics.Single().Location.ShouldEqual(location);
        Catch.Exception(() => new UiSyntaxAdmission().VisitApplication(syntax)).ShouldBeOfExactType<UnsupportedUiSyntax>();
    }
}
