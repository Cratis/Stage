// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_mismatching_query_expectation : Specification
{
    string _query = null!;

    void Because()
    {
        var source = register_project_scenarios.WithPriorProjectAndSubsetExpectation.Replace(
            "          result\n            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
            "          result\n            projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n            name = \"Wrong query result\"",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Success.ShouldBeTrue();
        _query = Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject_is_queried.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_assert_the_wrong_value_rather_than_seeding_it() => _query.ShouldContain("_result.Name == new ProjectName(\"Wrong query result\")");
    [Fact] void should_seed_the_actual_event_from_the_command() => _query.ShouldContain("new ProjectName(\"Screenplay\")");
}
