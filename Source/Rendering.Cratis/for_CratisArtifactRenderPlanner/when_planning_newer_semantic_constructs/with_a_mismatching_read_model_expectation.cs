// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_mismatching_read_model_expectation : Specification
{
    string _projection = null!;

    void Because()
    {
        var source = register_project_scenarios.WithPriorProjectAndSubsetExpectation.Replace(
            "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
            "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Wrong project name\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Success.ShouldBeTrue();
        _projection = Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject_is_projected.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_assert_the_wrong_value_rather_than_silently_accepting_it() => _projection.ShouldContain(".Name.ShouldEqual(new ProjectName(\"Wrong project name\"))");
    [Fact] void should_not_seed_the_wrong_value_as_the_actual_event() => _projection.ShouldNotContain("new ProjectRegistered(new ProjectId(Guid.Parse(\"3fa85f64-5717-4562-b3fc-2c963f66afa6\")), new ProjectName(\"Wrong project name\"))");
}
