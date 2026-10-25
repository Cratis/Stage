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

    [Fact] void should_refuse_the_historical_revision_even_with_current_properties() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-025").Artifact.ShouldEqual(_event.Id);
    [Fact] void should_also_refuse_the_evolved_event() => rendering_refusals.Original(_plan, "STAGE-ESM-026").Artifact.ShouldEqual(_event.Id);
    [Fact] void should_report_the_incompatible_reducer_context() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Artifact == _request.TypedContextDescriptors.Single().OperationId);
    [Fact] void should_also_report_transition_analysis() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-023" && diagnostic.Artifact == _request.Model.Application.Id).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_omit_the_refused_owners_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
    [Fact] void should_record_partial_admission() => rendering_refusals.Partial(_plan);
}
