// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_an_evolved_event_and_an_unsupported_specification : given.a_collecting_render_request
{
    void Establish()
    {
        var specification = _registerProject.Specifications.First();
        _request = WithChange(_registerProject with
        {
            Events = [.. _registerProject.Events.Select(@event => @event with { Revision = new(2) })],
            Specifications = [specification with { GivenReadModels = specification.ThenReadModels }]
        });
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_the_event_revision() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-026" && diagnostic.Artifact == _registerProject.Events.Single().Id);
    [Fact] void should_also_report_specification_admission() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == _registerProject.Specifications.First().Id);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
