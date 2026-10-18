// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_envelope_refusal : given.a_refused_specification
{
    void Establish() => _request = _request with { ImplementationRequirements = default };
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_refuse_the_entire_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_keep_the_envelope_error() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_list_omissions() => _plan.Refusals.ShouldBeEmpty();
}
