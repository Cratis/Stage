// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderTypeResolver;

public class when_resolving_types : for_RenderModelBuilder.given.a_response_only_command
{
    RenderTypeResolver _resolver = null!;

    void Establish()
    {
        var application = _request.Model.Application with
        {
            Concepts = [new(Id("concept"), "Label", SemanticPrimitiveType.Text, [], []), new(Id("enum"), "Status", SemanticPrimitiveType.Text, ["Open", "Closed"], [])],
            Types = [new(Id("composite"), "Envelope", _command.Properties), new(Id("broken"), "Broken", [_command.Properties[0] with { Type = new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, Id("missing"), false, false) }])]
        };
        _resolver = new(application);
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Uuid)]
    [InlineData(SemanticPrimitiveType.Text)]
    [InlineData(SemanticPrimitiveType.WholeNumber)]
    [InlineData(SemanticPrimitiveType.DecimalNumber)]
    [InlineData(SemanticPrimitiveType.Boolean)]
    [InlineData(SemanticPrimitiveType.Date)]
    [InlineData(SemanticPrimitiveType.DateTime)]
    void should_resolve_primitives_without_losing_cardinality(SemanticPrimitiveType primitive) => _resolver.Resolve(new(SemanticTypeReferenceKind.Primitive, primitive, default, true, true)).ShouldEqual(new(RenderTypeKind.Primitive, primitive, null, true, true));

    [Fact] void should_resolve_concept_targets() => _resolver.Resolve(new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, Id("concept"), false, false)).Target.ShouldEqual(Id("concept"));
    [Fact] void should_resolve_enum_concepts() => _resolver.Resolve(new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, Id("enum"), false, false)).ShouldEqual(new(RenderTypeKind.Concept, SemanticPrimitiveType.Text, Id("enum"), false, false));
    [Fact] void should_resolve_composite_targets() => _resolver.Resolve(new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, Id("composite"), false, false)).Kind.ShouldEqual(RenderTypeKind.Composite);
    [Fact] void should_reject_unknown_primitives() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Unknown, default, false, false)));
    [Fact] void should_reject_unknown_kinds() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.Unknown, SemanticPrimitiveType.Text, default, false, false)));
    [Fact] void should_reject_missing_concepts() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, Id("missing"), false, false)));
    [Fact] void should_reject_missing_composites() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, Id("missing"), false, false)));
    [Fact] void should_reject_a_composite_as_a_concept() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.Concept, SemanticPrimitiveType.Unknown, Id("composite"), false, false))).Message.ShouldContain(Id("composite").ToString());
    [Fact] void should_reject_a_concept_as_a_composite() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, Id("concept"), false, false))).Message.ShouldContain(Id("concept").ToString());
    [Fact] void should_reject_an_unclassified_nested_property() => Assert.Throws<InvalidArtifactRenderContract>(() => _resolver.Resolve(new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, Id("broken"), false, false))).Message.ShouldContain(Id("missing").ToString());
}
