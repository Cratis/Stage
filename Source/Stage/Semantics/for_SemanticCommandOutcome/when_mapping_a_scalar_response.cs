// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_a_scalar_response : Specification
{
    CommandResult _result = null!;

    void Because() => _result = SemanticCommandOutcome.Map(
        new SemanticAccepted(SemanticWorld.Empty, [], []) { Response = new SemanticScalarExecutionResponse(SemanticValue.Text("hello")) },
        CorrelationId.NotSet,
        null,
        "command");

    [Fact] void should_carry_the_scalar_value() => ((CommandResult<object?>)_result).Response.ShouldEqual("hello");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
