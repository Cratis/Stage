// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_produced_scoped_join : a_generated_application
{
    internal static string Source => when_rendering_scoped_projections.ScopedSource
        .Replace("        name ProjectName\n        produces ProjectRegistered", "        name ProjectName\n        joinedName ProjectName\n        produces ProjectRegistered", StringComparison.Ordinal)
        .Replace("      event ProjectRegistered", "        produces ProjectNamed\n          for projectId\n          name = joinedName\n      event ProjectRegistered", StringComparison.Ordinal)
        .Replace("          name = \"Screenplay\"\n        then ProjectRegistered", "          name = \"Screenplay\"\n          joinedName = \"B\"\n        then ProjectRegistered", StringComparison.Ordinal)
        .Replace("          name = \"Screenplay\"\n        then query ProjectById", "          name = \"B\"\n        then query ProjectById", StringComparison.Ordinal)
        .Replace("            name = \"Screenplay\"\n      specification LookingUpPinnedProject", "            name = \"B\"\n      specification LookingUpPinnedProject", StringComparison.Ordinal)
        .Replace("        then readmodel ProjectSummary", "        then ProjectNamed\n          name = \"B\"\n        then readmodel ProjectSummary", StringComparison.Ordinal);

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    string _result = null!;

    async Task Because()
    {
        await Run("joined-debug.log", "build", "-c", "Debug", "-warnaserror");
        await Run("joined-release.log", "build", "-c", "Release", "-warnaserror");
        _result = await Run("joined-test.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_pass_the_joined_read_model_and_query_specs() => _result.ShouldContain("Passed!");
    [Fact] void should_replay_the_join_in_the_read_model_spec() => Assert.Contains("Events(new ProjectNamed(", ReadGeneratedFile("Projects/Registration/RegisterProject/when_registering_aproject_is_projected.cs"));
    [Fact] void should_replay_the_join_in_the_query_spec() => Assert.Contains("Events(new ProjectNamed(", ReadGeneratedFile("Projects/Registration/RegisterProject/when_registering_aproject_is_queried.cs"));
}
#endif
