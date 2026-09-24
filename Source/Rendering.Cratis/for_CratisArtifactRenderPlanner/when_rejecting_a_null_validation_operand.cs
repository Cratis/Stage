// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_null_validation_operand : Specification
{
    bool _supported;

    void Because() => _supported = SemanticValidationRendering.CanRender(
        new(default, SemanticValidationRuleKind.Equal, SemanticValue.Null, null));

    [Fact] void should_not_emit_an_incomplete_equality_expression() => _supported.ShouldBeFalse();
}
