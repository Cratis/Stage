// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_SemanticValueWire;

public class when_serializing_a_predicate_response : Specification
{
    [Fact]
    void should_round_trip_the_complete_response_with_an_initialized_message_array()
    {
        var response = new BodyWorkerResponse("call") { Evaluation = new("satisfied", SemanticValueWire.Write(SemanticValue.Boolean(true))) };
        var json = JsonSerializer.Serialize(response, BodyWorkerMessages.Json);
        var read = JsonSerializer.Deserialize<BodyWorkerResponse>(json, BodyWorkerMessages.Json)!;
        read.Id.ShouldEqual("call");
        read.Evaluation!.Messages.IsDefault.ShouldBeFalse();
        read.Evaluation.Messages.ShouldBeEmpty();
        SemanticValueWire.Read(read.Evaluation.Value!.Value).ShouldEqual(SemanticValue.Boolean(true));
    }
}
