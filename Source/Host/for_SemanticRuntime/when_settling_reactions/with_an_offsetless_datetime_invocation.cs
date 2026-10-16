// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_offsetless_datetime_invocation : a_reaction_runtime
{
    protected override string Model => StartSource + """

            slice Automation FollowUp
              command CompleteWork
                id String identifier
                at DateTime
                validate
                  at >= "2026-01-01T00:00:00Z"
                produces Completed
                  for id
              reaction Worker
                when Started
                  id
                  invokes CompleteWork
                    id = id
                    at = "2026-01-01T00:00:00.0000000Z"
              event Completed
        """;

    protected override SemanticExecutionPlan Compile()
    {
        var plan = base.Compile();

        // Valid source literals require offsets; inject malformed invocation input after contract validation.
        var expression = (SemanticValueExpression)plan.Reactions.Single().Triggers.Single().Invokes.Single().Mappings.Single(mapping => mapping.Source is SemanticValueExpression).Source;
        typeof(SemanticValueExpression).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(expression, SemanticValue.Text("2026-01-01T00:00:00"));
        return plan;
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_invoked_input() => _result.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_preserve_the_validation_message() => ((SemanticRejected)_result).Details.ShouldEqual("DateTime command property 'at' requires an explicit offset or Z.");
    [Fact] void should_report_validation_rather_than_contract() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_keep_only_the_initiating_fact() => FactNames(_result.World.Facts).ShouldContainOnly(["Started"]);
}
