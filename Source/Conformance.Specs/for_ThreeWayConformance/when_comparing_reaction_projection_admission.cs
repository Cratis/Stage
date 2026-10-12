// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_reaction_projection_admission : a_projected_reaction
{
    [Theory]
    [InlineData("flat", SemanticSpecificationOutcome.Passed)]
    [InlineData("literal", SemanticSpecificationOutcome.Unsupported)]
    [InlineData("duplicate", SemanticSpecificationOutcome.Unsupported)]
    public async Task should_project_or_refuse_without_a_false_failure(string shape, SemanticSpecificationOutcome outcome)
    {
        CompileProjected(shape: shape);
        await Execute();
        Assert.True(_reference.Passed, string.Join(';', _reference.Failures));
        Assert.Equal(outcome, _result.Outcome);
        if (outcome == SemanticSpecificationOutcome.Unsupported) Assert.Equal(StageExecutionCapability.Projection, _result.Unsupported!.Capability);
    }

    [Fact]
    public async Task should_never_falsely_pass_absence_for_a_mismatched_payload_key()
    {
        CompileProjected("\"two\"", "then no readmodel Marker for \"two\"");
        await Execute();
        Assert.False(_reference.Passed);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, _result.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, _result.Unsupported!.Capability);
    }
}
