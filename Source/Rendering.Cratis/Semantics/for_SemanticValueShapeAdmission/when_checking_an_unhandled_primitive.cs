// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValueShapeAdmission;

public class when_checking_an_unhandled_primitive : given.a_value_shape
{
    string? _refusal;
    string? _renderingRefusal;

    void Because()
    {
        var type = SemanticTypeReference.ForPrimitive((SemanticPrimitiveType)99);
        _refusal = SemanticValueShapeAdmission.Refusal(_context, type);
        _renderingRefusal = SemanticValueShapeAdmission.Refusal(_context, type, deferUnhandledPrimitives: true);
    }

    [Fact] void should_refuse_execution_and_host_admission() => _refusal.ShouldEqual("The value has an unresolved type.");
    [Fact] void should_defer_to_the_renderer_primitive_diagnostic() => _renderingRefusal.ShouldBeNull();
}
