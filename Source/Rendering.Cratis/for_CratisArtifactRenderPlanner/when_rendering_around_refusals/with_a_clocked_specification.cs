// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_clocked_specification : given.a_refused_specification
{
    void Establish()
    {
        _request = OptIn(WithSource(Source.Replace("        when RegisterProject\n", "        given clock \"2027-03-04T12:00:00.0000001Z\"\n        when RegisterProject\n", StringComparison.Ordinal)));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_complete_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_list_the_clock_refusal() => _plan.Refusals.ShouldContain(refusal => refusal.Codes.Contains("STAGE-ESM-024"));
    [Fact] void should_omit_all_refused_sources() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).Intersect(_plan.Refusals.Select(refusal => refusal.Artifact)).ShouldBeEmpty();
}
