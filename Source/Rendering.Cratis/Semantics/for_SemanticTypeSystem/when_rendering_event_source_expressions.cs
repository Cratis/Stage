// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTypeSystem;

public class when_rendering_event_source_expressions : a_register_project_render_request
{
    string _identity = null!;
    string _ordinaryConcept = null!;
    string _text = null!;
    string _uuid = null!;
    string _negativeWholeNumber = null!;
    string _negativeDecimal = null!;

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        var types = new SemanticTypeSystem(context);
        var identityType = SemanticTypeReference.ForConcept(context.Concepts.Values.Single(_ => _.Name == "ProjectId").Id);
        var valueType = SemanticTypeReference.ForConcept(context.Concepts.Values.Single(_ => _.Name == "ProjectName").Id);
        _identity = types.EventSourceExpression("Destination", identityType);
        _ordinaryConcept = types.EventSourceExpression(types.Value(SemanticValue.Text("stream"), valueType), valueType);
        _text = types.EventSourceExpression("Destination", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        _uuid = types.EventSourceExpression("Destination", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid));
        var wholeNumber = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber);
        var decimalNumber = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber);
        _negativeWholeNumber = types.EventSourceExpression(types.Value(SemanticValue.Number(-42), wholeNumber), wholeNumber);
        _negativeDecimal = types.EventSourceExpression(types.Value(SemanticValue.Number(-12.5m), decimalNumber), decimalNumber);
    }

    [Fact] void should_use_the_identity_concepts_implicit_conversion() => _identity.ShouldEqual("Destination");
    [Fact] void should_use_to_string_for_an_ordinary_concept() => _ordinaryConcept.ShouldEqual("new EventSourceId((new ProjectName(\"stream\")).ToString())");
    [Fact] void should_use_the_text_implicit_conversion() => _text.ShouldEqual("Destination");
    [Fact] void should_use_the_uuid_implicit_conversion() => _uuid.ShouldEqual("Destination");
    [Fact] void should_parenthesize_a_negative_integer_specification_value() => _negativeWholeNumber.ShouldEqual("new EventSourceId((-42).ToString())");
    [Fact] void should_parenthesize_a_negative_decimal_specification_value() => _negativeDecimal.ShouldEqual("new EventSourceId((-12.5m).ToString())");
}
