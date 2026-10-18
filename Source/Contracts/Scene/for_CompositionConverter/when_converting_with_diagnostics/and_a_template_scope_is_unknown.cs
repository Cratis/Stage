// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CompositionConverter.when_converting_with_diagnostics;

public class and_a_template_scope_is_unknown : Specification
{
    [Fact]
    public void should_omit_only_the_unknown_scope()
    {
        var location = new SourceLocation(4, 3, "templates.play");
        var diagnostics = new ConversionDiagnostics();
        var result = CompositionConverter.Metadata(null, null, true, ["module", "unknown", "slice"], [], diagnostics, "Workspace", location);

        result.Scopes!.ToArray().ShouldEqual([TemplateScope.Module, TemplateScope.Slice]);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-COMPOSITION-002");
        diagnostics.All.Single().Path.ShouldEqual("Workspace");
        diagnostics.All.Single().Location.ShouldEqual(location);
        Catch.Exception(() => CompositionConverter.Metadata(null, null, true, ["unknown"])).ShouldBeOfExactType<UnknownCompositionValue>();
    }
}
