// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_seeded_query_result(with_a_wrong_seeded_query_result.context fixture) : IClassFixture<with_a_wrong_seeded_query_result.context>
{
    [Fact] void should_fail_the_generated_seeded_query_specification() => fixture.Error!.Message.ShouldContain("should_have_name");
    [Fact] void should_report_the_wrong_expectation() => fixture.Error!.Message.ShouldContain("Failed");

    public class context : a_generated_application
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

        async Task Because()
        {
            await Run("seeded-wrong-debug.log", "build", "-c", "Debug", "-warnaserror");
            Error = await Catch.Exception(() => Run("seeded-wrong-test.log", "test", "-c", "Debug", "--no-build"));
        }

        public Exception? Error { get; private set; }
    }
}
#endif
