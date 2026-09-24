// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_produced_join_result : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = with_a_produced_scoped_join.Source.Replace(
            "        then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"B\"",
            "        then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Exception? _failure;

    async Task Because()
    {
        await Run("wrong-join-debug.log", "build", "-c", "Debug", "-warnaserror");
        _failure = await Catch.Exception(() => Run("wrong-join-test.log", "test", "-c", "Debug", "--no-build"));
    }

    [Fact] void should_fail_the_stale_name_expectation() => _failure!.Message.ShouldContain("should_project_name");
}
#endif
