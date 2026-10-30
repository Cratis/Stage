// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_sourceless_given_event : given.a_refused_specification
{
    void Establish()
    {
        _request = OptIn(WithSource(Source.Replace(
            "      specification RegisteringAProject\n",
            "      specification RegisteringAProject\n        given ProjectRegistered\n          projectId = \"0b5f2f3c-1d3e-4c8f-9a51-6b2f0e7a1c42\"\n          name = \"Earlier\"\n",
            StringComparison.Ordinal)));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_complete_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_list_the_sourceless_refusal() => _plan.Refusals.ShouldContain(refusal => refusal.Artifact == _refused && refusal.Codes.Contains("STAGE-ESM-051"));
    [Fact] void should_omit_the_refused_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
}
