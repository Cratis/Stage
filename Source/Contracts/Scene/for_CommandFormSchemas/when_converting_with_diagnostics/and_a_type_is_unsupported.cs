// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandFormSchemas.when_converting_with_diagnostics;

public class and_a_type_is_unsupported : Specification
{
    [Theory]
    [InlineData(false, "STAGE-SCENE-SCHEMA-001")]
    [InlineData(true, "STAGE-SCENE-SCHEMA-002")]
    public void should_report_the_type_and_keep_other_schemas_convertible(bool primitive, string code)
    {
        var location = new SourceLocation(8, 5, "commands.play");
        var application = new SemanticApplication(default, "Projects", [], [], []);
        var supported = new SemanticProperty(default, "Title", new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false), false);
        var unsupported = supported with { Type = primitive ? supported.Type with { Primitive = (SemanticPrimitiveType)99 } : supported.Type with { Kind = (SemanticTypeReferenceKind)99 } };
        var diagnostics = new ConversionDiagnostics();

        CommandFormSchemas.For(application, [unsupported], diagnostics, "Register", location).ShouldBeNull();
        CommandFormSchemas.For(application, [supported], diagnostics, "Rename", location).ShouldNotBeNull();
        diagnostics.All.Single().Code.ShouldEqual(code);
        diagnostics.All.Single().Path.ShouldEqual("Register.Title");
        diagnostics.All.Single().Location.ShouldEqual(location);
        Catch.Exception(() => CommandFormSchemas.For(application, [unsupported])).ShouldBeOfExactType<UnsupportedCommandFormSchema>();
    }

    [Fact]
    public void should_report_an_unsupported_concept_primitive()
    {
        var location = new SourceLocation(8, 5, "concepts.play");
        var application = new SemanticApplication(default, "Projects", [new SemanticConcept(default, "Title", (SemanticPrimitiveType)99, [], [])], [], []);
        var diagnostics = new ConversionDiagnostics();

        CommandFormSchemas.For(application, [], diagnostics, "Register", location).ShouldBeNull();
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-SCHEMA-002");
        diagnostics.All.Single().Construct.ShouldEqual("concept primitive 99");
        diagnostics.All.Single().Location.ShouldEqual(location);
        Catch.Exception(() => CommandFormSchemas.For(application, [])).ShouldBeOfExactType<UnsupportedCommandFormSchema>();
    }
}
