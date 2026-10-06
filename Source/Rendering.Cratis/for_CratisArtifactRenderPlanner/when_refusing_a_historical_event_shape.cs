// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_a_historical_event_shape : a_v4_reducer
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var descriptor = _request.TypedContextDescriptors.Single();
        var member = descriptor.Members.Single(member => member.Name == "Event");
        _request = _request with
        {
            TypedContextDescriptors = [descriptor with
            {
                Members = descriptor.Members.Replace(member, member with
                {
                    Source = member.Source with { EventRevision = EventContractRevision.Initial }
                })
            }]
        };
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_refuse_the_historical_revision_even_with_current_properties() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-025");
    [Fact] void should_locate_the_event() => _plan.Diagnostics.Single().Artifact.ShouldEqual(_event.Id);
    [Fact] void should_emit_no_partial_plan() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_fail_admission() => _plan.Success.ShouldBeFalse();
}
