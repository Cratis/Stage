// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_atomic_batch_rejection : a_reaction_runtime
{
    protected override string Model => "reaction batches atomic\n" + StartSource + """

            slice Automation FollowUp
              event Completed
              constraint Once
                unique event Completed
                message "Already completed"
              reaction Worker
                when Started
                  produces Completed
                  produces Completed
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_atomic_model() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_reject_the_later_production() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_preserve_the_authored_refusal_text() => ((SemanticRejected)_result).Details.ShouldEqual("Already completed");
    [Fact] void should_append_neither_the_command_nor_its_reactions() => _batches.ShouldBeEmpty();
    [Fact] void should_preserve_the_original_world() => _result.World.Facts.ShouldBeEmpty();
}
