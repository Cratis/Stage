// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_seeded_query_result : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = when_rendering_scoped_projections.ScopedSource.Replace(
            "result\n            projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n            name = \"Pinned\"",
            "result\n            projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n            name = \"Wrong\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Exception? _error;

    async Task Because()
    {
        await Run("seeded-wrong-debug.log", "build", "-c", "Debug", "-warnaserror");
        _error = await Catch.Exception(() => Run("seeded-wrong-test.log", "test", "-c", "Debug", "--no-build"));
    }

    [Fact] void should_fail_the_generated_seeded_query_specification() => _error!.Message.ShouldContain("should_return_the_seeded_read_model");
    [Fact] void should_report_the_wrong_expectation() => _error!.Message.ShouldContain("Failed");
}
#endif
