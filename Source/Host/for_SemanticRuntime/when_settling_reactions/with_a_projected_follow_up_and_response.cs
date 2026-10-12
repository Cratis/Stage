// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_projected_follow_up_and_response : a_reaction_runtime
{
    protected override string Model => StartSource.Replace("        produces Started", "        returns id\n        produces Started", StringComparison.Ordinal) + ProducingSource + """

            slice StateView Summary
              readmodel WorkSummary
                id String
                amount Decimal
              query WorkById => WorkSummary?
                by id String
              projection Summary => WorkSummary
                from Completed key id
                  amount = amount
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_preserve_the_initiating_response() => ((SemanticScalarExecutionResponse)((SemanticAccepted)_result).Response!).Value.ShouldEqual(SemanticValue.Text("one"));
    [Fact] void should_commit_the_follow_up_projection() => _result.World.ReadModels.Single().Values.Single(value => value.TargetProperty == _plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "amount").Id).Value.ShouldEqual(SemanticValue.Number(7));
    [Fact] async Task should_expose_the_committed_read_model() => (await _runtime.ReadModels(_plan.ReadModels.Values.Single().Id)).Length.ShouldEqual(1);
}
