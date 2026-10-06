// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_historical_event_properties : a_v4_reducer
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
                    Type = member.Type with
                    {
                        Properties = [.. _event.PriorRevisions.Single().Properties.Select(property => new SemanticContextProperty(property.Name, property.Id, property.Type))]
                    }
                })
            }]
        };
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_refuse_historical_property_identities_even_with_the_current_revision() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-025");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
