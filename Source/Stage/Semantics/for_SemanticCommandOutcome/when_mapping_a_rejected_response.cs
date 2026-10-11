// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_a_rejected_response : Specification
{
    CommandResult _result = null!;
    void Because() => _result = SemanticCommandOutcome.Map(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "Rejected"), CorrelationId.NotSet, null, "C");
    [Fact] void should_have_no_response_in_the_http_result() => JsonSerializer.SerializeToElement(_result, _result.GetType()).TryGetProperty("Response", out _).ShouldBeFalse();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
}
