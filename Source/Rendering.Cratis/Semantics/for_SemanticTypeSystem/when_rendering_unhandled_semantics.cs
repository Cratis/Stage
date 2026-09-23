// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTypeSystem;

public class when_rendering_unhandled_semantics : a_register_project_render_request
{
    Exception? _primitive;
    Exception? _notSet;
    Exception? _type;
    Exception? _value;

    void Because()
    {
        var types = new SemanticTypeSystem(new SemanticApplicationContext(_request, _options));
        _primitive = Catch.Exception(() => SemanticTypeSystem.Primitive((SemanticPrimitiveType)99));
        _notSet = Catch.Exception(() => SemanticTypeSystem.NotSet((SemanticPrimitiveType)99));
        _type = Catch.Exception(() => types.Type(new((SemanticTypeReferenceKind)99, SemanticPrimitiveType.Unknown, default, false, false)));
        _value = Catch.Exception(() => types.Value(SemanticValue.Array([]), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)));
    }

    [Fact] void should_reject_unhandled_primitive() => _primitive.ShouldBeOfExactType<UnsupportedSemanticRendering>();
    [Fact] void should_reject_unhandled_sentinel() => _notSet.ShouldBeOfExactType<UnsupportedSemanticRendering>();
    [Fact] void should_reject_unhandled_type_reference() => _type.ShouldBeOfExactType<UnsupportedSemanticRendering>();
    [Fact] void should_reject_unhandled_value() => _value.ShouldBeOfExactType<UnsupportedSemanticRendering>();
}
