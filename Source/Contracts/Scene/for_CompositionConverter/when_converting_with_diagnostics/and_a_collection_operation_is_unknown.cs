// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Exposure;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CompositionConverter.when_converting_with_diagnostics;

public class and_a_collection_operation_is_unknown : Specification
{
    [Fact]
    public void should_omit_only_the_unknown_operation()
    {
        var location = new SourceLocation(4, 3, "exposures.play");
        var property = new ExposedPropertySyntax("items", "items", location) { IsCollection = true, Operations = ["add", "unknown", "remove"] };
        var exposure = new ExposureSyntax("Shell", [property], location);
        var diagnostics = new ConversionDiagnostics();
        var result = CompositionConverter.Exposure(exposure, diagnostics);

        result.Properties.Single().Operations!.ToArray().ShouldEqual([CollectionOperation.Add, CollectionOperation.Remove]);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-COMPOSITION-001");
        diagnostics.All.Single().Path.ShouldEqual("Shell");
        diagnostics.All.Single().Location.ShouldEqual(location);
        Catch.Exception(() => CompositionConverter.Exposure(exposure)).ShouldBeOfExactType<UnknownCompositionValue>();
    }
}
