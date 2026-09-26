// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_wrong_projected_value : a_command_only_plan
{
    SemanticSpecificationRunRecord _result = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var original = _original.ThenReadModels.Single();
        var identifier = _plan.ReadModels[original.ReadModel].Properties.Single(property => property.IsIdentifier).Id;
        var first = original.Values.First(value => value.TargetProperty != identifier && value.Value is SemanticTextValue);
        var mismatched = original with { Values = [.. original.Values.Select(value => value.TargetProperty == first.TargetProperty ? value with { Value = SemanticValue.Text("not the projected value") } : value)] };
        var specification = _specification with { ThenReadModels = [mismatched] };
        var plan = WithProjection(specification);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
    }

    [Fact] void should_fail_in_both_engines() => Assert.True(!_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Failed);
    [Fact] void should_report_the_projected_mismatch() => Assert.Contains(_result.Failures, failure => failure.Contains("read model", StringComparison.Ordinal));
}
