// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_renaming_conditional_local_collisions : Specification
{
    [Theory]
    [InlineData("stageEvents")]
    [InlineData("stageProduces0")]
    [InlineData("stageProduces1")]
    public void should_rename_a_local_that_clashes_with_a_generated_parameter(string name)
    {
        var model = invoice_model.Compile(when_rendering_conditional_generated_productions.Source.Replace("receipt", name, StringComparison.Ordinal));
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        var slice = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Start.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        slice.ShouldContain($"var {name}1 =");
        slice.ShouldContain($" {name})");
    }
}
