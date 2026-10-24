// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_creating_an_unverified_gap : given.a_gap
{
    ArtifactRenderPlan _plan = null!;
    ArtifactImplementationGap _roundTripped = null!;

    void Because()
    {
        var gap = _gap with { Origin = ArtifactImplementationGapOrigin.Unverified };
        _plan = ArtifactRenderPlan.Create(_request, _artifacts, [], [gap]);
        _roundTripped = JsonSerializer.Deserialize<ArtifactImplementationGap>(JsonSerializer.Serialize(gap, StageJson.Options), StageJson.Options)!;
    }

    [Fact] void should_accept_the_additive_origin() => _plan.ImplementationGaps[0].Origin.ShouldEqual(ArtifactImplementationGapOrigin.Unverified);
    [Fact] void should_remain_successful_without_errors() => _plan.Success.ShouldBeTrue();
    [Fact] void should_be_partial_despite_having_no_errors() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_preserve_the_origin_through_json() => _roundTripped.Origin.ShouldEqual(ArtifactImplementationGapOrigin.Unverified);
    [Fact] void should_include_the_origin_in_the_digest() => Digest([_roundTripped]).ShouldNotEqual(Digest([_gap]));
    [Fact] void should_leave_described_gaps_partial_too() => ArtifactRenderPlan.Create(_request, _artifacts, [], [_gap]).Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_leave_gap_free_plans_complete() => ArtifactRenderPlan.Create(_request, _artifacts, []).Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_leave_gap_free_error_plans_refused() => ArtifactRenderPlan.Create(_request, [], [new("TEST", ArtifactRenderDiagnosticSeverity.Error, "Refused", _request.Model.Application.Id)]).Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
}
