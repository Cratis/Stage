// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_conditional_local_collisions : Specification
{
    [Theory]
    [InlineData("stageEvents")]
    [InlineData("stageProduces0")]
    [InlineData("stageProduces1")]
    public void should_refuse_a_generated_parameter_with_a_reserved_local_name(string name)
    {
        var model = invoice_model.Compile(when_rendering_conditional_generated_productions.Source.Replace("receipt", name, StringComparison.Ordinal));
        var plan = invoice_model.Plan(model);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-038");
        plan.Artifacts.ShouldBeEmpty();
    }
}
