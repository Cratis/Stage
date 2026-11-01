// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_builderless_state_views;

public class and_planning_is_strict : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = builderless_state_view_model.Plan(builderless_state_view_model.Builderless, strict: true);

    [Fact] void should_fail() => _plan.Success.ShouldBeFalse();
    [Fact] void should_promote_the_degradation_to_an_error() => _plan.Diagnostics.Single(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
}
