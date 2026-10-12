// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_a_record_response : Specification
{
    CommandResult _result = null!;

    void Because() => _result = SemanticCommandOutcome.Map(
        new SemanticAccepted(SemanticWorld.Empty, [], [])
        {
            Response = new SemanticRecordExecutionResponse([new("ProjectId", SemanticValue.Text("hello")), new("count", SemanticValue.Number(2))])
        },
        CorrelationId.NotSet,
        null,
        "command");

    [Fact] void should_use_camel_case_field_names() => ((Dictionary<string, object?>)((CommandResult<object?>)_result).Response!).Keys.ShouldContainOnly("projectId", "count");
    [Fact] void should_carry_field_values() => ((Dictionary<string, object?>)((CommandResult<object?>)_result).Response!)["projectId"].ShouldEqual("hello");
}
