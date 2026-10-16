// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticGeneratedValues;

public class when_reporting_a_generated_fixture_defect : Specification
{
    [Theory]
    [InlineData("bad shape", true)]
    [InlineData("01arz3ndektsv4rrffq69g5fav", true)]
    [InlineData("00arz3ndektsv4rrffq69g5fav", false)]
    public void should_translate_only_the_evaluators_generated_shape_or_rule_rejection(string fixture, bool defect)
    {
        var plan = compiled_plan.From("""
            concept Code : String
              generate ulid case lower
              validate
                matches "^(?<portable>0[02-9a-hjkmnp-tv-z][0-9a-hjkmnp-tv-z]{24}|[1-7][0-9a-hjkmnp-tv-z]{25})$"
            module M
              feature F
                slice StateChange S
                  command C
                    code Code generated
                    returns code
            """);
        var command = plan.Commands.Values.Single();
        var request = SemanticExecutionRequest.Create(command.Id, [], []) with
        {
            GeneratedValues = [new(command.Properties.Single().Id, SemanticValue.Text(fixture))]
        };
        var outcome = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        SemanticExecutionResult reported = null!;
        var error = Catch.Exception(() => reported = SemanticGeneratedValues.ReportDefect(command, plan.Model.Application, outcome));
        if (defect)
        {
            ((SemanticRejected)outcome).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            error.ShouldBeOfExactType<SemanticCommandExecutionFailed>();
            error.Message.ShouldEqual(((SemanticRejected)outcome).Details);
        }
        else
        {
            error.ShouldBeNull();
            outcome.ShouldBeOfExactType<SemanticAccepted>();
            reported.ShouldBeSame(outcome);
        }
    }
}
