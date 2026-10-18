// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting_with_diagnostics;

public class and_a_nested_directive_is_unknown : Specification
{
    [Fact]
    public void should_keep_the_parent_and_supported_siblings()
    {
        var location = new SourceLocation(6, 3, "screens.play");
        var section = new ScreenSectionSyntax("Details", [new UnknownDirective(location), new ScreenTitleSyntax("Retained", location)], location);
        var diagnostics = new ConversionDiagnostics();
        var result = ScreenDirectiveConverter.Convert([section], "Projects", null, null, diagnostics);

        ((ExternalComponent)result.Single()).Slots["content"].Count.ShouldEqual(1);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-DIRECTIVE-001");
        diagnostics.All.Single().Location.ShouldEqual(location);
        diagnostics.All.Single().Path.ShouldEqual("Projects.0-section.0-unsupported");
        Catch.Exception(() => ScreenDirectiveConverter.Convert([section], "Projects")).ShouldBeOfExactType<UnknownScreenDirective>();
    }

    sealed record UnknownDirective(SourceLocation Location) : ScreenDirectiveSyntax(Location);
}
