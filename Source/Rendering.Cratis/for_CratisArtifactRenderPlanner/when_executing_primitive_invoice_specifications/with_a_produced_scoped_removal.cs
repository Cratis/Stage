// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_produced_scoped_removal : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = with_a_produced_scoped_join.Source
            .Replace("      event ProjectRegistered", "        produces ProjectRemoved\n          for projectId\n      event ProjectRegistered", StringComparison.Ordinal)
            .Replace("        then readmodel ProjectSummary", "        then ProjectRemoved\n        then readmodel ProjectSummary", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Exception? _failure;

    async Task Because()
    {
        await Run("removed-debug.log", "build", "-c", "Debug", "-warnaserror");
        _failure = await Catch.Exception(() => Run("removed-test.log", "test", "-c", "Debug", "--no-build"));
    }

    [Fact] void should_fail_the_removed_read_model_expectation() => _failure!.Message.ShouldContain("should_project_name");
    [Fact] void should_replay_the_removal() => ReadGeneratedFile("Projects/Registration/RegisterProject/when_registering_aproject_is_projected.cs").ShouldContain("Events(new ProjectRemoved(");
}
#endif
