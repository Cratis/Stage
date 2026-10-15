// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_malformed_requests : given.a_composite_input
{
    void Establish() => Prepare("RequiredDetails", "{\"count\":7}");

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("wrong scalar")]
    [InlineData("duplicate input")]
    [InlineData("missing input")]
    public void should_match_reference_contract_rejections_before_rules(string shape)
    {
        var input = _specification.When!.Values[^1];
        var composite = (SemanticCompositeValue)input.Value;
        var malformed = shape switch
        {
            "duplicate" => SemanticValue.Composite([composite.Properties[0], composite.Properties[0]]),
            "unknown" => SemanticValue.Composite([composite.Properties[0] with { TargetProperty = _command.Properties[0].Id }]),
            "missing" => SemanticValue.Composite([]),
            _ => SemanticValue.Text("wrong")
        };
        ImmutableArray<SemanticPropertyValue> values = shape switch
        {
            "duplicate input" => [input, input],
            "missing input" => [input],
            _ => [.. _specification.When.Values.Select(value => value.TargetProperty == input.TargetProperty ? value with { Value = malformed } : value)]
        };
        var request = new SemanticExecutionRequest(_command.Id, values, [], []);
        var reference = Assert.IsType<SemanticRejected>(new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request));
        reference.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        SemanticConceptValues.RequestFailure(_plan, _command, values).ShouldEqual(reference.Details);
    }
}
