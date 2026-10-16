// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValueShapeAdmission;

public class when_checking_an_unhandled_concept_primitive : given.a_value_shape
{
    string? _refusal;
    bool _renderingTypeExists;

    void Establish()
    {
        var concepts = (Dictionary<SemanticId, SemanticConcept>)_context.Concepts;
        var concept = concepts.Values.Single();
        concepts[concept.Id] = concept with { Primitive = (SemanticPrimitiveType)99 };
    }

    void Because()
    {
        _refusal = SemanticValueShapeAdmission.Refusal(_context, _type);
        var concept = _context.Concepts.Values.Single();
        _renderingTypeExists = SemanticCratisAdmission.TypeExists(_context, _type with { Kind = SemanticTypeReferenceKind.Concept, Target = concept.Id });
    }

    [Fact] void should_refuse_execution_and_host_admission() => _refusal.ShouldEqual("The value has an unresolved type.");
    [Fact] void should_defer_to_the_renderer_primitive_diagnostic() => _renderingTypeExists.ShouldBeTrue();
}
