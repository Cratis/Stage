// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_generated_concept_defect : a_reaction_runtime
{
    Exception _error = null!;

    protected override string Model => """
        concept Code : String
          generate ulid case lower
          validate
            matches "^(?<portable>0[02-9a-hjkmnp-tv-z][0-9a-hjkmnp-tv-z]{24}|[1-7][0-9a-hjkmnp-tv-z]{25})$"
        """ + "\n" + StartSource + """

            slice Automation FollowUp
              command CompleteWork
                code Code generated
                returns code
              reaction Worker
                when Started
                  invokes CompleteWork
        """;

    async Task Because() => _error = await Catch.Exception(async () => await Execute());

    [Fact] void should_fail_as_a_server_exception() => _error.ShouldBeOfExactType<SemanticCommandExecutionFailed>();
    [Fact] void should_keep_the_reference_contract_message() => _error.Message.ShouldEqual("Generated fixture 'code' fails concept 'Code' rules");
    [Fact] void should_not_append_even_the_triggering_fact() => _batches.ShouldBeEmpty();
}
