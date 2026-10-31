// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_a_collision_on_the_root_event : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var owner = _registerProject.Events.Single();
        Replace(owner, "Name", "Name");
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_both_errors_on_the_same_event() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == _unsupportedEvent && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldEqual(2);
    [Fact] void should_refuse_the_whole_render() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_publish_no_degradation() => _plan.Degradations.ShouldBeEmpty();
}
