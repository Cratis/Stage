// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_checking_unhandled_primitives : given.a_query_shape
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_defer_to_the_renderer_primitive_diagnostic(bool isConcept)
    {
        var context = Context();
        var type = SemanticTypeReference.ForPrimitive((SemanticPrimitiveType)99);
        if (isConcept)
        {
            var concepts = (Dictionary<SemanticId, SemanticConcept>)context.Concepts;
            var concept = concepts.Values.First();
            concepts[concept.Id] = concept with { Primitive = (SemanticPrimitiveType)99 };
            type = type with { Kind = SemanticTypeReferenceKind.Concept, Target = concept.Id };
        }
        SemanticCratisAdmission.TypeExists(context, type).ShouldBeTrue();
    }
}
