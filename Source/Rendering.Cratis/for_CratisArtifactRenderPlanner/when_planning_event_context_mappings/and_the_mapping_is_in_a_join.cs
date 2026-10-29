// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_mapping_is_in_a_join : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = event_context_projection_model.Plan(event_context_projection_model.InAJoin);

    [Fact] void should_fail() => _plan.Success.ShouldBeFalse();
    [Fact] void should_still_refuse_the_projection() => _plan.Diagnostics.Any(_ => _.Code == "STAGE-ESM-017" && _.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_degrade() => _plan.Diagnostics.Any(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).ShouldBeFalse();
}
