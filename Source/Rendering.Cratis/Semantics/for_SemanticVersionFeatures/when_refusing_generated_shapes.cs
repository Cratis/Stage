// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_refusing_generated_shapes : Specification
{
    const string Contract = "Screenplay requires a generated property to be a required, non-collection generatable concept (PLAY0483).";

    SemanticCommand _command = null!;
    SemanticConcept _concept = null!;

    void Establish()
    {
        var model = invoice_model.Compile("concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        returns id");
        _command = SemanticVersionFeatures.Slices(model.Application).Single().Commands.Single();
        _concept = model.Application.Concepts.Single();
    }

    [Fact] void should_name_the_optional_shape() => Message(_command.Properties.Single().Type with { IsOptional = true }).ShouldEqual($"Command 'C' generates 'id' as an optional concept; {Contract}");
    [Fact] void should_name_the_collection_shape() => Message(_command.Properties.Single().Type with { IsCollection = true }).ShouldEqual($"Command 'C' generates 'id' as a collection of concepts; {Contract}");
    [Fact] void should_name_the_primitive_shape() => Message(_command.Properties.Single().Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.Uuid }).ShouldEqual($"Command 'C' generates 'id' as a primitive; {Contract}");
    [Fact] void should_name_the_composite_type_shape() => Message(_command.Properties.Single().Type with { Kind = SemanticTypeReferenceKind.CompositeType }).ShouldEqual($"Command 'C' generates 'id' as a composite type; {Contract}");
    [Fact] void should_name_an_optional_primitive_shape() => Message(_command.Properties.Single().Type with { Kind = SemanticTypeReferenceKind.Primitive, IsOptional = true }).ShouldEqual($"Command 'C' generates 'id' as an optional primitive; {Contract}");

    string Message(SemanticTypeReference type) => SemanticVersionFeatures.UnsupportedGenerated(_command with { Properties = [_command.Properties.Single() with { Type = type }] }, [_concept]).Single().Message;
}
