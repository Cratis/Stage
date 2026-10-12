// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_generated_invoked_value : a_reaction_runtime
{
    protected override string Model => "concept CompletionId : Uuid\n" + StartSource + """

            slice Automation FollowUp
              command CompleteWork
                completionId CompletionId generated identifier
                amount Decimal
                produces Completed
                  for completionId
                  completionId = completionId
                  amount = amount
              reaction Worker
                when Started
                  amount
                  invokes CompleteWork
                    amount = amount
              event Completed
                completionId CompletionId
                amount Decimal
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_generated_invocation() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_invoked_fact_in_the_same_batch() => FactNames(_batches.Single()).ShouldContainOnly(["Started", "Completed"]);
    [Fact] void should_generate_a_uuid_destination() => Guid.TryParseExact(((SemanticTextValue)_result.World.Facts[1].Destination).Value, "D", out _).ShouldBeTrue();
    [Fact] void should_map_the_generated_value_into_the_event() => _result.World.Facts[1].Values.Select(value => value.Value).ShouldContain(_result.World.Facts[1].Destination);
    [Fact] void should_retain_the_reaction_origin() => _result.World.Facts[1].ReactionOrigin.ShouldEqual(_plan.Reactions.Single().Id);
}
