// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticGenerationShapes;

public class when_checking_generation_rule_fixtures : Specification
{
    SemanticConcept _concept = null!;

    void Establish() => _concept = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan()).Application.Concepts.Single(concept => concept.Generation?.Kind == "implementation");

    [Theory]
    [InlineData(SemanticPrimitiveType.Text, true)]
    [InlineData(SemanticPrimitiveType.Uuid, false)]
    [InlineData(SemanticPrimitiveType.WholeNumber, false)]
    void should_admit_only_string_generation(SemanticPrimitiveType primitive, bool supported) => SemanticGenerationShapes.Supported(_concept with { Primitive = primitive }).ShouldEqual(supported);
    [Theory]
    [InlineData("")]
    [InlineData("provided")]
    [InlineData("tenant:order\nnext")]
    void should_accept_any_string_without_invoking_the_body(string value) => SemanticGenerationShapes.Matches(_concept, SemanticValue.Text(value)).ShouldBeTrue();
    [Fact] void should_refuse_non_string_fixtures() => SemanticGenerationShapes.Matches(_concept, SemanticValue.Number(7)).ShouldBeFalse();
}
