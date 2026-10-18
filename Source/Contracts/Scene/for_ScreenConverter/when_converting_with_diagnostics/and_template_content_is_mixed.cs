// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting_with_diagnostics;

public class and_template_content_is_mixed : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_omit_only_the_unsupported_screen(bool secondTemplate)
    {
        var location = new SourceLocation(6, 3, "screens.play");
        var template = new ScreenTemplateReferenceSyntax("Workspace", [], location);
        var screen = new ScreenSyntax("Bad", null, [template, secondTemplate ? template : new ScreenTitleSyntax("Mixed", location)], location);
        var good = new ScreenSyntax("Good", null, [new ScreenTitleSyntax("Good", location)], location);
        var slice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [], [], [], [screen, good], [], [], location);
        var module = new ModuleSyntax("Projects", [], [new FeatureSyntax("Browsing", [], [slice], location)], location);
        var syntax = new ApplicationSyntax([], [], [], [module], location);

        var result = new ScreenplaySceneVisitor().Convert(syntax);

        result.Screens.Single().Name.ShouldEqual("Good");
        result.ConversionDiagnostics.Single().Code.ShouldEqual("STAGE-SCENE-SCREEN-001");
        result.ConversionDiagnostics.Single().Location.ShouldEqual(location);
        result.ConversionDiagnostics.Single().Path.ShouldEqual("Projects.Browsing.Bad");
        Catch.Exception(() => ScreenConverter.Convert(screen, "Shell", [], [])).ShouldBeOfExactType<UnsupportedScreenContent>();
    }
}
