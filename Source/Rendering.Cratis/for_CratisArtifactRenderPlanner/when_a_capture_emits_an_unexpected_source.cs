// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_capture_emits_an_unexpected_source : given.a_translation_specification_application
{
    TargetInvocationException _exception = null!;

    async Task Because()
    {
        _model = Transform(_model, slice => slice.Name == "TrackShipments" ? slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                ThenEvents = [.. specification.ThenEvents.Select(expected => expected with
                {
                    EventSource = expected.EventSource! with { Value = new SemanticTextValue("11111111-1111-1111-1111-111111111111") }
                })]
            })]
        } : slice);
        Render(clientOnly: true);
        _exception = await Assert.ThrowsAsync<TargetInvocationException>(() => RunGenerated("when_receiving_a_shipment"));
    }

    [Fact] void should_fail_when_the_emitted_event_source_does_not_match_the_expectation() => _exception.InnerException!.GetType().Namespace.ShouldEqual("Xunit.Sdk");
}
#endif
