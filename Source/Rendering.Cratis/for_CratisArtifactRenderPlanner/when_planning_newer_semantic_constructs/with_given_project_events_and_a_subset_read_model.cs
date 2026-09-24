// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_given_project_events_and_a_subset_read_model : Specification
{
    string _command = null!;
    string _projection = null!;
    string _query = null!;
    bool _success;

    void Because()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(register_project_scenarios.WithPriorProjectAndSubsetExpectation));
        _success = plan.Success;
        if (_success)
        {
            _command = Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject.cs", StringComparison.Ordinal)).Bytes.AsSpan());
            _projection = Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject_is_projected.cs", StringComparison.Ordinal)).Bytes.AsSpan());
            _query = Encoding.UTF8.GetString(plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject_is_queried.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        }
    }

    [Fact] void should_admit_subset_read_model_expectations() => _success.ShouldBeTrue();
    [Fact] void should_seed_the_given_event_on_its_own_source() => _command.ShouldContain("ForEventSource(new ProjectId(Guid.Parse(\"4fa85f64-5717-4562-b3fc-2c963f66afa7\")))");
    [Fact] void should_select_the_expected_project_among_multiple_instances() => _projection.ShouldContain("InstanceForEventSourceId(new ProjectId(Guid.Parse(\"3fa85f64-5717-4562-b3fc-2c963f66afa6\")))");
    [Fact] void should_assert_only_the_authored_project_property() => _projection.ShouldNotContain("should_project_name");
    [Fact] void should_seed_the_query_from_projected_events() => _query.ShouldContain("Returns(_scenario.InstanceForEventSourceId");
    [Fact] void should_not_mock_the_expected_result_as_the_actual_state() => _query.ShouldNotContain("Returns(_expected)");
}
