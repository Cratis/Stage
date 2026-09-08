// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTypeSystem;

public class when_determining_common_imports : Specification
{
    SemanticTypeReference _concept = null!;
    SemanticTypeReference _composite = null!;
    bool _conceptDeclaration;
    bool _compositeDeclaration;
    bool _primitiveDeclaration;
    bool _nullConcept;
    bool _nullComposite;
    bool _emptyConcepts;
    bool _nullElements;
    bool _populatedConcepts;
    bool _populatedComposites;

    void Establish()
    {
        // Import decisions depend on the type shape, not the identity catalog or any global concept count.
        _concept = SemanticTypeReference.ForConcept(default, isCollection: true, isOptional: true);
        _composite = SemanticTypeReference.ForCompositeType(default, isCollection: true);
    }

    void Because()
    {
        _conceptDeclaration = SemanticTypeSystem.DeclarationNeedsCommon(_concept);
        _compositeDeclaration = SemanticTypeSystem.DeclarationNeedsCommon(_composite);
        _primitiveDeclaration = SemanticTypeSystem.DeclarationNeedsCommon(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true, isOptional: true));
        _nullConcept = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Null, _concept);
        _nullComposite = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Null, _composite);
        _emptyConcepts = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Array([]), _concept);
        _nullElements = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Array([SemanticValue.Null]), _concept);
        _populatedConcepts = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Array([SemanticValue.Text("value")]), _concept);
        _populatedComposites = SemanticTypeSystem.ValueNeedsCommon(SemanticValue.Array([SemanticValue.Composite([])]), _composite);
    }

    [Fact] void should_import_optional_concept_collection_declarations() => _conceptDeclaration.ShouldBeTrue();
    [Fact] void should_import_composite_collection_declarations() => _compositeDeclaration.ShouldBeTrue();
    [Fact] void should_not_import_primitive_collection_declarations() => _primitiveDeclaration.ShouldBeFalse();
    [Fact] void should_not_import_for_a_null_concept_value() => _nullConcept.ShouldBeFalse();
    [Fact] void should_not_import_for_a_null_composite_value() => _nullComposite.ShouldBeFalse();
    [Fact] void should_not_import_for_an_empty_concept_collection_value() => _emptyConcepts.ShouldBeFalse();
    [Fact] void should_not_import_for_null_collection_elements() => _nullElements.ShouldBeFalse();
    [Fact] void should_import_for_a_constructed_concept_collection_element() => _populatedConcepts.ShouldBeTrue();
    [Fact] void should_import_for_a_constructed_composite_collection_element() => _populatedComposites.ShouldBeTrue();
}
