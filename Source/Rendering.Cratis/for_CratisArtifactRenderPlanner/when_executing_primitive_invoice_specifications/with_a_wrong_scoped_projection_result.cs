// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_scoped_projection_result : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = when_rendering_scoped_projections.ScopedSource.Replace(
            "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Wrong\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Exception? _error;

    async Task Because()
    {
        await Run("scoped-wrong-debug.log", "build", "-c", "Debug", "-warnaserror");
        _error = await Catch.Exception(() => Run("scoped-wrong-test.log", "test", "-c", "Debug", "--no-build"));
    }

    [Fact] void should_fail_the_generated_scoped_projection_specification() => _error!.Message.ShouldContain("should_project_name");
    [Fact] void should_report_the_wrong_expectation() => _error!.Message.ShouldContain("Failed");
}
#endif
