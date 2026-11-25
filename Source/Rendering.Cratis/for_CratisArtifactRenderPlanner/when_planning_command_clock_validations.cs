// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_command_clock_validations : Specification
{
    [Fact]
    void should_compile_and_plan_every_generated_clock_validation_specification()
    {
        var model = invoice_model.Compile(when_executing_command_clock_validations.Source);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        plan.Artifacts.Count(artifact => artifact.RelativePath.Contains("/when_", StringComparison.Ordinal)).ShouldEqual(4);
    }
}
#endif
