// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Lifecycles.for_AbilityInputs;

public class when_composing_an_input : Specification
{
    JsonObject _input = null!;

    void Because()
    {
        var description = JsonNode.Parse(
            """
            [
              { "input": "reason", "value": "late" },
              { "input": "overdue", "subject": "overdue" },
              { "input": "booking", "node": "Book", "field": "bookingId" },
              { "input": "missing", "subject": "notThere" },
              { "input": "unreached", "node": "Later", "field": "bookingId" }
            ]
            """)!.AsArray();
        var subject = new JsonObject { ["Overdue"] = true };
        var nodes = new JsonObject { ["Book"] = new JsonObject { ["bookingId"] = "B-1" } };
        _input = AbilityInputs.Compose(description, subject, nodes);
    }

    [Fact] void should_give_the_literal() => _input["reason"]!.GetValue<string>().ShouldEqual("late");
    [Fact] void should_read_the_subject_without_regard_to_case() => _input["overdue"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_read_the_field_an_earlier_node_returned() => _input["booking"]!.GetValue<string>().ShouldEqual("B-1");
    [Fact] void should_leave_out_a_subject_member_that_has_no_value() => _input.ContainsKey("missing").ShouldBeFalse();
    [Fact] void should_leave_out_a_node_that_has_not_run() => _input.ContainsKey("unreached").ShouldBeFalse();
}
